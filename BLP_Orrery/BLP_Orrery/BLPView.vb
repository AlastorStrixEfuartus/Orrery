Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Module BLPView

    Public NotInheritable Class BlpFile : Implements IDisposable

#Region "Private Fields"
        Private Const MaxMipMapLevels As Integer = 16
        Private Type As UInteger ' compression: 0 = JPEG Compression, 1 = Uncompressed or DirectX Compression
        Private Encoding As Byte ' 1 = Uncompressed, 2 = DirectX Compressed
        Private AlphaDepth As Byte ' 0 = no alpha, 1 = 1 Bit, 4 = Bit (only DXT3), 8 = 8 Bit Alpha
        Private AlphaEncoding As Byte ' 0: DXT1 alpha (0 or 1 Bit alpha), 1 = DXT2/3 alpha (4 Bit), 7: DXT4/5 (interpolated alpha)
        Private HasMipmaps As Byte ' If 1 then there are Mipmaps
        Private Width As Integer ' X Resolution of the biggest Mipmap
        Private Height As Integer ' Y Resolution of the biggest Mipmap
        Private MipMapOffsets As UInteger() = New UInteger(MaxMipMapLevels - 1) {} ' Offset for every Mipmap level. If 0 = no more mitmap level
        Private MipMapSize As UInteger() = New UInteger(MaxMipMapLevels - 1) {} ' Size for every level
        Private MipMapErrors As String() = New String(MaxMipMapLevels - 1) {}
        Private PaletteBGRA As ARGBColor8() = New ARGBColor8(255) {} ' The color-palette for non-compressed pictures
        Private Str As Stream ' Reference of the stream
        Private IsValidVersion As Boolean = True ' used to pass by if the found image is BLP2 or not 
#End Region

#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls
        Public Sub Dispose() Implements IDisposable.Dispose        ' This code added by Visual Basic to correctly implement the disposable pattern.
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            If Not disposedValue Then
                If Str IsNot Nothing Then
                    Str.Close()
                    Str = Nothing
                End If
            End If
            disposedValue = True  ' TODO: uncomment the following line if Finalize() is overridden above.

        End Sub
#End Region
        Public Structure ARGBColor8
            Public red As Byte
            Public green As Byte
            Public blue As Byte
            Public alpha As Byte

            Public Shared Sub ConvertToBGRA(ByRef pixel As Byte())
                Dim tmp As Byte
                For i As Integer = 0 To pixel.Length - 1 Step 4
                    tmp = pixel(i)
                    pixel(i) = pixel(i + 2)
                    pixel(i + 2) = tmp
                Next
            End Sub
        End Structure

#Region "Private Methods"
        Private Function ClampMipmapLevel(ByVal MipmapLevel As Integer) As Integer
            Dim count As Integer = MipMapCount
            If count <= 0 Then Return 0
            If MipmapLevel < 0 Then Return 0
            If MipmapLevel >= count Then Return count - 1
            Return MipmapLevel
        End Function

        Private Sub EnsureReadableMipmap(ByVal MipmapLevel As Integer)
            If Not IsMipmapReadable(MipmapLevel) Then
                Throw New InvalidDataException("BLP mipmap " & MipmapLevel.ToString() & " is not readable: " & GetMipmapStatusText(MipmapLevel))
            End If
        End Sub

        Private Function GetMipDimension(ByVal BaseDimension As Integer, ByVal MipmapLevel As Integer) As Integer
            Dim divisor As Integer = CInt(Math.Pow(2, MipmapLevel))
            Return Math.Max(1, BaseDimension \ divisor)
        End Function

        Private Function GetCheckedPixelByteCount(ByVal ImageWidth As Integer, ByVal ImageHeight As Integer, ByVal BytesPerPixel As Integer, ByVal ErrorMessage As String) As Integer
            If ImageWidth <= 0 OrElse ImageHeight <= 0 OrElse BytesPerPixel <= 0 Then Throw New InvalidDataException(ErrorMessage)

            Dim byteCount As Long = CLng(ImageWidth) * CLng(ImageHeight) * CLng(BytesPerPixel)
            If byteCount <= 0 OrElse byteCount > Integer.MaxValue Then Throw New InvalidDataException(ErrorMessage)

            Return CInt(byteCount)
        End Function

        Private Function GetUncompressedAlpha(ByVal Data As Byte(), ByVal AlphaOffset As Integer, ByVal PixelIndex As Integer, ByVal PaletteAlpha As Byte) As Byte
            If AlphaDepth = 0 Then Return 255

            Select Case AlphaDepth
                Case 1
                    Dim byteIndex As Integer = AlphaOffset + (PixelIndex \ 8)
                    If byteIndex >= Data.Length Then Return PaletteAlpha
                    Dim bitValue As Integer = (Data(byteIndex) >> (PixelIndex Mod 8)) And 1
                    Return If(bitValue = 0, CByte(0), CByte(255))
                Case 4
                    Dim byteIndex As Integer = AlphaOffset + (PixelIndex \ 2)
                    If byteIndex >= Data.Length Then Return PaletteAlpha
                    Dim alphaValue As Integer
                    If (PixelIndex And 1) = 0 Then
                        alphaValue = Data(byteIndex) And &HF
                    Else
                        alphaValue = (Data(byteIndex) >> 4) And &HF
                    End If
                    Return CByte((alphaValue << 4) Or alphaValue)
                Case 8
                    Dim byteIndex As Integer = AlphaOffset + PixelIndex
                    If byteIndex >= Data.Length Then Return PaletteAlpha
                    Return Data(byteIndex)
            End Select

            Return PaletteAlpha
        End Function

        Private Function GetDxtFlags() As Integer
            Select Case AlphaEncoding
                Case 0
                    Return CInt(DXTFlags.DXT1) Or If(AlphaDepth > 0, CInt(DXTFlags.DXT1Alpha), 0)
                Case 1
                    Return CInt(DXTFlags.DXT3)
                Case 7
                    Return CInt(DXTFlags.DXT5)
            End Select

            If AlphaDepth > 1 Then
                Return CInt(DXTFlags.DXT3)
            End If

            Return CInt(DXTFlags.DXT1) Or If(AlphaDepth > 0, CInt(DXTFlags.DXT1Alpha), 0)
        End Function

        Private Function GetPictureUncompressedByteArray(ByVal MipmapLevel As Integer) As Byte()
            MipmapLevel = ClampMipmapLevel(MipmapLevel)

            Dim mipWidth As Integer = GetMipmapWidth(MipmapLevel)
            Dim mipHeight As Integer = GetMipmapHeight(MipmapLevel)
            Dim pixelCount As Integer = GetCheckedPixelByteCount(mipWidth, mipHeight, 1, "BLP palettized mipmap dimensions are too large.")
            Dim pic As Byte() = New Byte(GetCheckedPixelByteCount(mipWidth, mipHeight, 4, "BLP palettized mipmap dimensions are too large.") - 1) {}
            Dim data As Byte() = GetPictureData(MipmapLevel)

            If data.Length < pixelCount Then
                Throw New InvalidDataException("BLP mipmap data is shorter than the expected pixel index data.")
            End If

            For i As Integer = 0 To pixelCount - 1
                Dim paletteIndex As Byte = data(i)
                Dim pixelOffset As Integer = i * 4

                pic(pixelOffset) = PaletteBGRA(paletteIndex).red
                pic(pixelOffset + 1) = PaletteBGRA(paletteIndex).green
                pic(pixelOffset + 2) = PaletteBGRA(paletteIndex).blue
                pic(pixelOffset + 3) = GetUncompressedAlpha(data, pixelCount, i, PaletteBGRA(paletteIndex).alpha)
            Next

            Return pic
        End Function

        Private Function GetPictureRawBgraByteArray(ByVal MipmapLevel As Integer) As Byte()
            MipmapLevel = ClampMipmapLevel(MipmapLevel)

            Dim mipWidth As Integer = GetMipmapWidth(MipmapLevel)
            Dim mipHeight As Integer = GetMipmapHeight(MipmapLevel)
            Dim expectedBytes As Integer = GetCheckedPixelByteCount(mipWidth, mipHeight, 4, "BLP RAW3 mipmap dimensions are too large.")
            Dim data As Byte() = GetPictureData(MipmapLevel)

            If data.Length < expectedBytes Then
                Throw New InvalidDataException("BLP RAW3 mipmap data is shorter than the expected BGRA pixel data.")
            End If

            Dim pic As Byte() = New Byte(expectedBytes - 1) {}

            For sourceOffset As Integer = 0 To expectedBytes - 1 Step 4
                pic(sourceOffset) = data(sourceOffset + 2)
                pic(sourceOffset + 1) = data(sourceOffset + 1)
                pic(sourceOffset + 2) = data(sourceOffset)
                pic(sourceOffset + 3) = If(AlphaDepth = 0, CByte(255), data(sourceOffset + 3))
            Next

            Return pic
        End Function

        Private Function GetPictureData(ByVal MipmapLevel As Integer) As Byte()
            If Str IsNot Nothing Then
                MipmapLevel = ClampMipmapLevel(MipmapLevel)
                EnsureReadableMipmap(MipmapLevel)

                Dim mipOffset As Long = CLng(MipMapOffsets(MipmapLevel))
                Dim mipSize As Long = CLng(MipMapSize(MipmapLevel))

                If mipSize = 0 Then Return New Byte() {}
                If mipSize > Integer.MaxValue Then Throw New InvalidDataException("BLP mipmap data is too large to read.")
                If mipOffset > Str.Length OrElse mipOffset + mipSize > Str.Length Then
                    Throw New InvalidDataException("BLP mipmap offset points past the end of the file.")
                End If

                Dim data As Byte() = New Byte(CInt(mipSize) - 1) {}
                Str.Position = mipOffset

                Dim bytesRead As Integer = 0
                While bytesRead < data.Length
                    Dim read As Integer = Str.Read(data, bytesRead, data.Length - bytesRead)
                    If read = 0 Then Throw New EndOfStreamException("Unexpected end of BLP mipmap data.")
                    bytesRead += read
                End While

                Return data
            End If

            Return New Byte() {}
        End Function

        Private Sub ValidateMipMapTable()
            If Str Is Nothing Then Throw New InvalidDataException("BLP stream is not available.")

            Dim foundMipMap As Boolean = False
            Dim foundEmptyEntry As Boolean = False
            Array.Clear(MipMapErrors, 0, MipMapErrors.Length)

            For i As Integer = 0 To MaxMipMapLevels - 1
                Dim mipOffset As UInteger = MipMapOffsets(i)
                Dim mipSize As UInteger = MipMapSize(i)
                Dim errorText As String = Nothing

                If mipOffset = 0UI AndAlso mipSize = 0UI Then
                    foundEmptyEntry = True
                    Continue For
                End If

                If mipOffset = 0UI Then
                    errorText = "Size is set but offset is 0."
                ElseIf mipSize = 0UI Then
                    errorText = "Offset is set but size is 0."
                ElseIf foundEmptyEntry Then
                    errorText = "Entry appears after an empty mipmap slot."
                ElseIf CLng(mipSize) > Str.Length Then
                    errorText = "Size exceeds file length."
                ElseIf mipSize > Integer.MaxValue Then
                    errorText = "Size is too large to read."
                ElseIf CLng(mipOffset) >= Str.Length OrElse CLng(mipOffset) + CLng(mipSize) > Str.Length Then
                    errorText = "Offset and size point past the end of the file."
                Else
                    foundMipMap = True
                End If

                MipMapErrors(i) = errorText
            Next

            If Not foundMipMap Then Throw New InvalidDataException("BLP file does not contain readable mipmap data.")
        End Sub

#End Region

#Region "Public Properties"
        Public ReadOnly Property MipMapCount As Integer
            Get
                Dim lastUsedIndex As Integer = -1
                For i As Integer = 0 To MipMapOffsets.Length - 1
                    If MipMapOffsets(i) <> 0UI OrElse MipMapSize(i) <> 0UI Then lastUsedIndex = i
                Next

                Return lastUsedIndex + 1
            End Get
        End Property

#End Region

#Region "public Methods"
        Public Sub New(ByVal _Stream As Stream)
            Str = _Stream
            Dim buffer As Byte() = New Byte(3) {}   ' Well, have to fix this... looks weird o.O

            If Str.Read(buffer, 0, 4) <> 4 Then Throw New EndOfStreamException("Unable to read the BLP header.")
            Dim MagicChar As String = New ASCIIEncoding().GetString(buffer)

            If Not (MagicChar = "BLP2") Then ' Checking for correct Magic-Code
                IsValidVersion = False
                Throw New InvalidDataException("Unsupported BLP format.")
            End If

            ' Reading type
            If Str.Read(buffer, 0, 4) <> 4 Then Throw New EndOfStreamException("Unable to read the BLP type.")
            Type = BitConverter.ToUInt32(buffer, 0)
            If Type <> 1 Then Throw New Exception("Invalid BLP-Type! Should be 1 but " & Type & " was found")

            ' Reading encoding, alphaBitDepth, alphaEncoding and hasMipmaps
            If Str.Read(buffer, 0, 4) <> 4 Then Throw New EndOfStreamException("Unable to read the BLP encoding.")
            Encoding = buffer(0)
            AlphaDepth = buffer(1)
            AlphaEncoding = buffer(2)
            HasMipmaps = buffer(3)

            ' Reading width
            If Str.Read(buffer, 0, 4) <> 4 Then Throw New EndOfStreamException("Unable to read the BLP width.")
            Width = BitConverter.ToInt32(buffer, 0)

            ' Reading height
            If Str.Read(buffer, 0, 4) <> 4 Then Throw New EndOfStreamException("Unable to read the BLP height.")
            Height = BitConverter.ToInt32(buffer, 0)
            If Width <= 0 OrElse Height <= 0 Then Throw New InvalidDataException("BLP image dimensions are invalid.")

            ' Reading MipmapOffset Array
            For i As Integer = 0 To MaxMipMapLevels - 1
                If _Stream.Read(buffer, 0, 4) <> 4 Then Throw New EndOfStreamException("Unable to read the BLP mipmap offsets.")
                MipMapOffsets(i) = BitConverter.ToUInt32(buffer, 0)
            Next
            ' Reading MipmapSize Array
            For i As Integer = 0 To MaxMipMapLevels - 1
                If Str.Read(buffer, 0, 4) <> 4 Then Throw New EndOfStreamException("Unable to read the BLP mipmap sizes.")
                MipMapSize(i) = BitConverter.ToUInt32(buffer, 0)
            Next

            ' When encoding is 1, there is no image compression and we have to read a color palette
            If Encoding = 1 Then

                ' Reading palette
                For i As Integer = 0 To 256 - 1
                    Dim color As Byte() = New Byte(3) {}
                    If Str.Read(color, 0, 4) <> 4 Then Throw New EndOfStreamException("Unable to read the BLP palette.")
                    PaletteBGRA(i).blue = color(0)
                    PaletteBGRA(i).green = color(1)
                    PaletteBGRA(i).red = color(2)
                    PaletteBGRA(i).alpha = color(3)
                Next
            ElseIf Encoding <> 2 AndAlso Encoding <> 3 Then
                Throw New InvalidDataException("Unsupported BLP encoding: " & Encoding)
            End If

            ValidateMipMapTable()
        End Sub

        Public Function GetImageBytes(ByVal MipmapLevel As Integer) As Byte()
            MipmapLevel = ClampMipmapLevel(MipmapLevel)
            EnsureReadableMipmap(MipmapLevel)

            Dim DecompressWidth As Integer = GetMipmapWidth(MipmapLevel)
            Dim DecompressHeight As Integer = GetMipmapHeight(MipmapLevel)
            Dim pic As Byte() = New Byte(GetCheckedPixelByteCount(DecompressWidth, DecompressHeight, 4, "BLP mipmap dimensions are too large to decode.") - 1) {}

            If Encoding = 2 Then
                DecompressImage(pic, DecompressWidth, DecompressHeight, GetPictureData(MipmapLevel), GetDxtFlags())
            ElseIf Encoding = 1 Then
                ' Using the palette to determine the color
                pic = GetPictureUncompressedByteArray(MipmapLevel)
            ElseIf Encoding = 3 Then
                pic = GetPictureRawBgraByteArray(MipmapLevel)
            Else
                Throw New InvalidDataException("Unsupported BLP encoding: " & Encoding)
            End If

            Return pic
        End Function

        Public Function GetBitmap(ByVal MipmapLevel As Integer) As Bitmap
            MipmapLevel = ClampMipmapLevel(MipmapLevel)
            EnsureReadableMipmap(MipmapLevel)

            Dim x As Integer = GetMipmapWidth(MipmapLevel), y As Integer = GetMipmapHeight(MipmapLevel)
            Dim bmp As New Bitmap(x, y, Imaging.PixelFormat.Format32bppArgb)
            Dim pic As Byte() = GetImageBytes(MipmapLevel) ' This bytearray stores the Pixel-Data

            ' Faster bitmap Data copy
            Dim bmpdata As Imaging.BitmapData = bmp.LockBits(New Rectangle(0, 0, x, y), Imaging.ImageLockMode.[WriteOnly], Imaging.PixelFormat.Format32bppArgb)
            ' when we want to copy the pixeldata directly into the bitmap, we have to convert them into BGRA befor doing so
            ARGBColor8.ConvertToBGRA(pic)
            Marshal.Copy(pic, 0, bmpdata.Scan0, pic.Length) ' copy! :D
            bmp.UnlockBits(bmpdata)

            Return bmp
        End Function

        Public Function GetBLPAlphaDepth() As Integer
            Return AlphaDepth
        End Function
        Public Function GetBLPAlphaEncoding() As Integer
            Return AlphaEncoding
        End Function
        Public Function GetBLPHasMipmap() As Integer
            Return HasMipmaps
        End Function
        Public Function GetBLPMipMapOffset() As UInteger()
            Return MipMapOffsets
        End Function
        Public Function GetBLPMipMapOffset(ByVal MipmapLevel As Integer) As UInteger
            MipmapLevel = ClampMipmapLevel(MipmapLevel)
            Return MipMapOffsets(MipmapLevel)
        End Function
        Public Function GetBLPMipMapSize() As UInteger()
            Return MipMapSize
        End Function
        Public Function GetBLPMipMapSize(ByVal MipmapLevel As Integer) As UInteger
            MipmapLevel = ClampMipmapLevel(MipmapLevel)
            Return MipMapSize(MipmapLevel)
        End Function
        Public Function IsMipmapReadable(ByVal MipmapLevel As Integer) As Boolean
            MipmapLevel = ClampMipmapLevel(MipmapLevel)
            If MipMapCount <= 0 Then Return False
            Return MipMapOffsets(MipmapLevel) <> 0UI AndAlso MipMapSize(MipmapLevel) <> 0UI AndAlso String.IsNullOrEmpty(MipMapErrors(MipmapLevel))
        End Function
        Public Function GetMipmapStatusText(ByVal MipmapLevel As Integer) As String
            MipmapLevel = ClampMipmapLevel(MipmapLevel)
            If MipMapCount <= 0 Then Return "Unused"
            If IsMipmapReadable(MipmapLevel) Then Return "OK"
            If Not String.IsNullOrEmpty(MipMapErrors(MipmapLevel)) Then Return "ERROR: " & MipMapErrors(MipmapLevel)
            Return "Unused"
        End Function
        Public Function GetReadableMipMapCount() As Integer
            Dim count As Integer = 0
            For i As Integer = 0 To MipMapCount - 1
                If IsMipmapReadable(i) Then count += 1
            Next
            Return count
        End Function
        Public Function GetFirstReadableMipmapIndex() As Integer
            For i As Integer = 0 To MipMapCount - 1
                If IsMipmapReadable(i) Then Return i
            Next
            Return -1
        End Function
        Public Function GetMipmapWidth(ByVal MipmapLevel As Integer) As Integer
            MipmapLevel = ClampMipmapLevel(MipmapLevel)
            Return GetMipDimension(Width, MipmapLevel)
        End Function
        Public Function GetMipmapHeight(ByVal MipmapLevel As Integer) As Integer
            MipmapLevel = ClampMipmapLevel(MipmapLevel)
            Return GetMipDimension(Height, MipmapLevel)
        End Function
        Public Function GetBLPEncoding() As Integer
            Return Encoding
        End Function
        Public Function GetBLPType() As Integer
            Return Type
        End Function
        Public Function GetBLPWidth() As Integer
            Return Width
        End Function
        Public Function GetBLPHeight() As Integer
            Return Height
        End Function
        Public Function GetIsValidVersion() As Boolean
            Return IsValidVersion
        End Function
#End Region
    End Class



End Module
