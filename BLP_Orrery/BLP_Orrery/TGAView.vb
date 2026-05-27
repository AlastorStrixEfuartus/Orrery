Imports System.IO
Imports System.Runtime.InteropServices

Module TGAView

    Public NotInheritable Class TgaFile

        Private Enum TgaImageType
            NoImage = 0
            ColorMapped = 1
            TrueColor = 2
            Grayscale = 3
            RleColorMapped = 9
            RleTrueColor = 10
            RleGrayscale = 11
        End Enum

        Private Structure RgbaColor
            Public R As Byte
            Public G As Byte
            Public B As Byte
            Public A As Byte
        End Structure

        Private ReadOnly Data As Byte()
        Private Width As Integer
        Private Height As Integer
        Private PixelDepth As Integer
        Private ImageType As TgaImageType
        Private ImageDescriptor As Byte
        Private AlphaBits As Integer
        Private ColorMapType As Integer
        Private ColorMapFirstEntry As Integer
        Private ColorMapLength As Integer
        Private ColorMapEntrySize As Integer
        Private PixelDataOffset As Integer
        Private Palette As RgbaColor()

        Public Sub New(SourceStream As Stream)
            If SourceStream Is Nothing Then Throw New ArgumentNullException("SourceStream")
            If SourceStream.Length < 18 Then Throw New InvalidDataException("TGA file is too small.")

            Using memory As New MemoryStream()
                SourceStream.CopyTo(memory)
                Data = memory.ToArray()
            End Using

            ParseHeader()
        End Sub

        Public Function GetBitmap() As Bitmap
            Dim rgba As Byte() = DecodePixels()
            ConvertRgbaToBgra(rgba)

            Dim bmp As New Bitmap(Width, Height, Imaging.PixelFormat.Format32bppArgb)
            Dim bmpData As Imaging.BitmapData = bmp.LockBits(New Rectangle(0, 0, Width, Height),
                                                              Imaging.ImageLockMode.WriteOnly,
                                                              Imaging.PixelFormat.Format32bppArgb)
            Try
                Marshal.Copy(rgba, 0, bmpData.Scan0, rgba.Length)
            Finally
                bmp.UnlockBits(bmpData)
            End Try

            Return bmp
        End Function

        Public Function GetTgaWidth() As Integer
            Return Width
        End Function

        Public Function GetTgaHeight() As Integer
            Return Height
        End Function

        Public Function GetPixelDepth() As Integer
            Return PixelDepth
        End Function

        Public Function GetAlphaChannelText() As String
            If AlphaBits > 0 Then Return AlphaBits.ToString() & " bits"
            If PixelDepth = 32 OrElse ImageType = TgaImageType.RleTrueColor AndAlso PixelDepth = 32 Then Return "8 bits"
            If ImageType = TgaImageType.Grayscale OrElse ImageType = TgaImageType.RleGrayscale Then
                If PixelDepth = 16 Then Return "8 bits"
            End If
            Return "None"
        End Function

        Public Function GetFormatName() As String
            Select Case ImageType
                Case TgaImageType.ColorMapped
                    Return "Color Mapped"
                Case TgaImageType.TrueColor
                    Return "True Color"
                Case TgaImageType.Grayscale
                    Return "Grayscale"
                Case TgaImageType.RleColorMapped
                    Return "RLE Color Mapped"
                Case TgaImageType.RleTrueColor
                    Return "RLE True Color"
                Case TgaImageType.RleGrayscale
                    Return "RLE Grayscale"
            End Select

            Return "Unknown"
        End Function

        Private Sub ParseHeader()
            Dim idLength As Integer = Data(0)
            ColorMapType = Data(1)
            ImageType = CType(Data(2), TgaImageType)
            ColorMapFirstEntry = ReadUInt16(3)
            ColorMapLength = ReadUInt16(5)
            ColorMapEntrySize = Data(7)
            Width = ReadUInt16(12)
            Height = ReadUInt16(14)
            PixelDepth = Data(16)
            ImageDescriptor = Data(17)
            AlphaBits = ImageDescriptor And &HF

            If Width <= 0 OrElse Height <= 0 Then Throw New InvalidDataException("TGA dimensions are invalid.")
            If Not IsSupportedImageType() Then Throw New InvalidDataException("Unsupported TGA image type: " & CInt(ImageType).ToString())
            If ColorMapType <> 0 AndAlso ColorMapType <> 1 Then Throw New InvalidDataException("Unsupported TGA color map type: " & ColorMapType.ToString())

            Dim offset As Integer = 18 + idLength
            If offset > Data.Length Then Throw New InvalidDataException("TGA image ID extends past the end of the file.")

            If IsColorMapped() Then
                If ColorMapType <> 1 Then Throw New InvalidDataException("TGA color-mapped image has no color map.")
                Palette = DecodePalette(offset)
                offset += GetColorMapByteSize()
            ElseIf ColorMapType = 1 Then
                offset += GetColorMapByteSize()
            End If

            If offset > Data.Length Then Throw New InvalidDataException("TGA color map extends past the end of the file.")
            PixelDataOffset = offset
        End Sub

        Private Function DecodePixels() As Byte()
            Dim pixels As Byte() = New Byte(CheckedToInteger(CLng(Width) * CLng(Height) * 4L, "TGA image is too large.") - 1) {}
            Dim sourceOffset As Integer = PixelDataOffset
            Dim pixelIndex As Integer = 0
            Dim pixelCount As Integer = Width * Height

            If IsRleEncoded() Then
                While pixelIndex < pixelCount
                    If sourceOffset >= Data.Length Then Throw New EndOfStreamException("Unexpected end of TGA RLE data.")

                    Dim packetHeader As Byte = Data(sourceOffset)
                    sourceOffset += 1

                    Dim runLength As Integer = (packetHeader And &H7F) + 1
                    Dim isRunPacket As Boolean = (packetHeader And &H80) <> 0

                    If isRunPacket Then
                        Dim color As RgbaColor = ReadPixel(sourceOffset)
                        sourceOffset += GetPixelStorageBytes()

                        For i As Integer = 0 To runLength - 1
                            WritePixel(pixels, pixelIndex, color)
                            pixelIndex += 1
                            If pixelIndex > pixelCount Then Throw New InvalidDataException("TGA RLE packet writes past the image bounds.")
                        Next
                    Else
                        For i As Integer = 0 To runLength - 1
                            Dim color As RgbaColor = ReadPixel(sourceOffset)
                            sourceOffset += GetPixelStorageBytes()
                            WritePixel(pixels, pixelIndex, color)
                            pixelIndex += 1
                            If pixelIndex > pixelCount Then Throw New InvalidDataException("TGA RLE packet writes past the image bounds.")
                        Next
                    End If
                End While
            Else
                While pixelIndex < pixelCount
                    Dim color As RgbaColor = ReadPixel(sourceOffset)
                    sourceOffset += GetPixelStorageBytes()
                    WritePixel(pixels, pixelIndex, color)
                    pixelIndex += 1
                End While
            End If

            Return pixels
        End Function

        Private Function DecodePalette(Offset As Integer) As RgbaColor()
            If ColorMapLength <= 0 Then Throw New InvalidDataException("TGA color map is empty.")

            Dim paletteColors As RgbaColor() = New RgbaColor(ColorMapLength - 1) {}
            Dim entryBytes As Integer = GetColorMapEntryBytes()
            Dim paletteOffset As Integer = Offset

            For i As Integer = 0 To ColorMapLength - 1
                paletteColors(i) = ReadColorValue(paletteOffset, ColorMapEntrySize, GetAlphaBitsForDepth(ColorMapEntrySize))
                paletteOffset += entryBytes
            Next

            Return paletteColors
        End Function

        Private Function ReadPixel(Offset As Integer) As RgbaColor
            If Offset < 0 OrElse Offset + GetPixelStorageBytes() > Data.Length Then Throw New EndOfStreamException("Unexpected end of TGA pixel data.")

            If IsColorMapped() Then
                Dim index As Integer = ReadPaletteIndex(Offset) - ColorMapFirstEntry
                If index < 0 OrElse Palette Is Nothing OrElse index >= Palette.Length Then Throw New InvalidDataException("TGA palette index is outside the color map.")
                Return Palette(index)
            End If

            If ImageType = TgaImageType.Grayscale OrElse ImageType = TgaImageType.RleGrayscale Then
                Dim color As New RgbaColor With {.R = Data(Offset), .G = Data(Offset), .B = Data(Offset), .A = 255}
                If PixelDepth = 16 Then color.A = Data(Offset + 1)
                Return color
            End If

            Return ReadColorValue(Offset, PixelDepth, AlphaBits)
        End Function

        Private Function ReadColorValue(Offset As Integer, BitsPerPixel As Integer, AlphaBitCount As Integer) As RgbaColor
            Dim bytesPerPixel As Integer = Math.Max(1, (BitsPerPixel + 7) \ 8)
            If Offset < 0 OrElse Offset + bytesPerPixel > Data.Length Then Throw New EndOfStreamException("Unexpected end of TGA color data.")

            Select Case BitsPerPixel
                Case 15, 16
                    Dim value As Integer = ReadUInt16(Offset)
                    Return New RgbaColor With {
                        .R = ScaleToByte((value >> 10) And &H1F, 31),
                        .G = ScaleToByte((value >> 5) And &H1F, 31),
                        .B = ScaleToByte(value And &H1F, 31),
                        .A = If(AlphaBitCount > 0, If((value And &H8000) <> 0, CByte(255), CByte(0)), CByte(255))
                    }
                Case 24
                    Return New RgbaColor With {.R = Data(Offset + 2), .G = Data(Offset + 1), .B = Data(Offset), .A = 255}
                Case 32
                    Return New RgbaColor With {.R = Data(Offset + 2), .G = Data(Offset + 1), .B = Data(Offset), .A = Data(Offset + 3)}
            End Select

            Throw New InvalidDataException("Unsupported TGA pixel depth: " & BitsPerPixel.ToString())
        End Function

        Private Sub WritePixel(ByRef Pixels As Byte(), PixelIndex As Integer, Color As RgbaColor)
            Dim sourceX As Integer = PixelIndex Mod Width
            Dim sourceY As Integer = PixelIndex \ Width
            Dim targetX As Integer = If((ImageDescriptor And &H10) <> 0, Width - 1 - sourceX, sourceX)
            Dim targetY As Integer = If((ImageDescriptor And &H20) <> 0, sourceY, Height - 1 - sourceY)
            Dim targetOffset As Integer = (targetY * Width + targetX) * 4

            Pixels(targetOffset) = Color.R
            Pixels(targetOffset + 1) = Color.G
            Pixels(targetOffset + 2) = Color.B
            Pixels(targetOffset + 3) = Color.A
        End Sub

        Private Function IsSupportedImageType() As Boolean
            Select Case ImageType
                Case TgaImageType.ColorMapped, TgaImageType.TrueColor, TgaImageType.Grayscale,
                     TgaImageType.RleColorMapped, TgaImageType.RleTrueColor, TgaImageType.RleGrayscale
                    Return True
            End Select

            Return False
        End Function

        Private Function IsColorMapped() As Boolean
            Return ImageType = TgaImageType.ColorMapped OrElse ImageType = TgaImageType.RleColorMapped
        End Function

        Private Function IsRleEncoded() As Boolean
            Return ImageType = TgaImageType.RleColorMapped OrElse ImageType = TgaImageType.RleTrueColor OrElse ImageType = TgaImageType.RleGrayscale
        End Function

        Private Function GetPixelStorageBytes() As Integer
            Return Math.Max(1, (PixelDepth + 7) \ 8)
        End Function

        Private Function GetColorMapEntryBytes() As Integer
            Return Math.Max(1, (ColorMapEntrySize + 7) \ 8)
        End Function

        Private Function GetColorMapByteSize() As Integer
            Return CheckedToInteger(CLng(ColorMapLength) * CLng(GetColorMapEntryBytes()), "TGA color map is too large.")
        End Function

        Private Function GetAlphaBitsForDepth(BitsPerPixel As Integer) As Integer
            If BitsPerPixel = 16 Then Return 1
            If BitsPerPixel = 32 Then Return 8
            Return 0
        End Function

        Private Function ReadPaletteIndex(Offset As Integer) As Integer
            Select Case PixelDepth
                Case 8
                    Return Data(Offset)
                Case 15, 16
                    Return ReadUInt16(Offset)
            End Select

            Throw New InvalidDataException("Unsupported TGA palette index depth: " & PixelDepth.ToString())
        End Function

        Private Function ReadUInt16(Offset As Integer) As Integer
            If Offset < 0 OrElse Offset + 2 > Data.Length Then Throw New EndOfStreamException("Unexpected end of TGA header.")
            Return Data(Offset) Or (CInt(Data(Offset + 1)) << 8)
        End Function

        Private Function ScaleToByte(Value As Integer, MaxValue As Integer) As Byte
            If MaxValue <= 0 Then Return 0
            Return CByte((Value * 255 + (MaxValue \ 2)) \ MaxValue)
        End Function

        Private Function CheckedToInteger(Value As Long, ErrorMessage As String) As Integer
            If Value <= 0 OrElse Value > Integer.MaxValue Then Throw New InvalidDataException(ErrorMessage)
            Return CInt(Value)
        End Function

        Private Sub ConvertRgbaToBgra(ByRef Pixels As Byte())
            For i As Integer = 0 To Pixels.Length - 1 Step 4
                Dim tmp As Byte = Pixels(i)
                Pixels(i) = Pixels(i + 2)
                Pixels(i + 2) = tmp
            Next
        End Sub

    End Class

End Module
