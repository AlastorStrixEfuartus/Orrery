Imports System.Collections.Generic
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text

Public Module PLTView

    Public NotInheritable Class PltFile

        Private Const HeaderLength As Integer = 24
        Private Const PixelStride As Integer = 2
        Private Const MaxDimension As Integer = 16384

        Private ReadOnly Data As Byte()
        Private ReadOnly LayerPresence As Boolean() = New Boolean(255) {}
        Private Width As Integer
        Private Height As Integer
        Private VersionText As String
        Private PixelCount As Integer
        Private PixelDataLength As Integer

        Public Sub New(SourceStream As Stream)
            If SourceStream Is Nothing Then Throw New ArgumentNullException("SourceStream")
            If SourceStream.Length < HeaderLength Then Throw New InvalidDataException("PLT file is too small.")

            Using memory As New MemoryStream()
                SourceStream.CopyTo(memory)
                Data = memory.ToArray()
            End Using

            ParseHeader()
        End Sub

        Public Function GetBitmap() As Bitmap
            Dim pixels As Byte() = New Byte(CheckedToInteger(CLng(Width) * CLng(Height) * 4L, "PLT image is too large.") - 1) {}

            For sourceY As Integer = 0 To Height - 1
                Dim targetY As Integer = (Height - 1) - sourceY

                For x As Integer = 0 To Width - 1
                    Dim sourceOffset As Integer = HeaderLength + (((sourceY * Width) + x) * PixelStride)
                    Dim value As Byte = Data(sourceOffset)
                    Dim layerId As Byte = Data(sourceOffset + 1)
                    Dim red As Integer = value
                    Dim green As Integer = value
                    Dim blue As Integer = value

                    ApplyLayerPreviewColor(layerId, red, green, blue)

                    Dim targetOffset As Integer = ((targetY * Width) + x) * 4
                    pixels(targetOffset) = CByte(blue)
                    pixels(targetOffset + 1) = CByte(green)
                    pixels(targetOffset + 2) = CByte(red)
                    pixels(targetOffset + 3) = 255
                Next
            Next

            Dim bmp As New Bitmap(Width, Height, PixelFormat.Format32bppArgb)
            Dim bmpData As BitmapData = bmp.LockBits(New Rectangle(0, 0, Width, Height),
                                                     ImageLockMode.WriteOnly,
                                                     PixelFormat.Format32bppArgb)
            Try
                Marshal.Copy(pixels, 0, bmpData.Scan0, pixels.Length)
            Finally
                bmp.UnlockBits(bmpData)
            End Try

            Return bmp
        End Function

        Public Function GetPltWidth() As Integer
            Return Width
        End Function

        Public Function GetPltHeight() As Integer
            Return Height
        End Function

        Public Function GetVersionText() As String
            If String.IsNullOrWhiteSpace(VersionText) Then Return "Unknown"
            Return VersionText
        End Function

        Public Function GetPixelDataOffset() As Integer
            Return HeaderLength
        End Function

        Public Function GetPixelDataLength() As Integer
            Return PixelDataLength
        End Function

        Public Function GetFormatName() As String
            Return "Luminance + Layer ID"
        End Function

        Public Function GetPreviewModeText() As String
            Return "Color-coded layer preview"
        End Function

        Public Function GetAlphaChannelText() As String
            Return "None"
        End Function

        Public Function GetLayerSummary() As String
            Dim layers As New List(Of String)()

            For i As Integer = 0 To LayerPresence.Length - 1
                If LayerPresence(i) Then layers.Add(i.ToString() & " " & GetLayerName(i))
            Next

            If layers.Count = 0 Then Return "None"
            If layers.Count <= 12 Then Return String.Join(", ", layers.ToArray())

            Dim visibleLayers As String() = layers.GetRange(0, 12).ToArray()
            Return String.Join(", ", visibleLayers) & ", +" & (layers.Count - 12).ToString() & " more"
        End Function

        Private Sub ParseHeader()
            If Not HasPltSignature() Then Throw New InvalidDataException("Unsupported PLT signature.")

            VersionText = ReadAscii(4, 4)
            Width = BitConverter.ToInt32(Data, 16)
            Height = BitConverter.ToInt32(Data, 20)

            If Width <= 0 OrElse Height <= 0 Then Throw New InvalidDataException("PLT dimensions are invalid.")
            If Width > MaxDimension OrElse Height > MaxDimension Then Throw New InvalidDataException("PLT dimensions are too large.")

            Dim pixelCountLong As Long = CLng(Width) * CLng(Height)
            Dim pixelBytesLong As Long = pixelCountLong * PixelStride
            Dim requiredLength As Long = HeaderLength + pixelBytesLong

            If pixelCountLong <= 0L OrElse pixelCountLong > Integer.MaxValue Then Throw New InvalidDataException("PLT image is too large.")
            If pixelBytesLong > Integer.MaxValue Then Throw New InvalidDataException("PLT pixel data is too large.")
            If requiredLength > Data.Length Then Throw New InvalidDataException("PLT pixel data ends before the declared image size.")

            PixelCount = CInt(pixelCountLong)
            PixelDataLength = CInt(pixelBytesLong)

            For i As Integer = 0 To PixelCount - 1
                LayerPresence(Data(HeaderLength + (i * PixelStride) + 1)) = True
            Next
        End Sub

        Private Function HasPltSignature() As Boolean
            Return Data.Length >= HeaderLength AndAlso
                   Data(0) = AscW("P"c) AndAlso
                   Data(1) = AscW("L"c) AndAlso
                   Data(2) = AscW("T"c)
        End Function

        Private Function ReadAscii(Offset As Integer, Length As Integer) As String
            If Offset < 0 OrElse Length <= 0 OrElse Offset + Length > Data.Length Then Return String.Empty
            Return Encoding.ASCII.GetString(Data, Offset, Length).Trim(ChrW(0), " "c)
        End Function

        Private Shared Sub ApplyLayerPreviewColor(LayerId As Byte, ByRef Red As Integer, ByRef Green As Integer, ByRef Blue As Integer)
            Select Case LayerId
                Case 1
                    Red = 0
                Case 2
                    Green = 0
                Case 3
                    Blue = 0
                Case 4
                    Red = 255
                Case 5
                    Green = 255
                Case 6
                    Blue = 255
                Case 7
                    Red = 255
                    Green = 255
                Case 8
                    Red = 255
                    Blue = 255
                Case 9
                    Green = 255
                    Blue = 255
            End Select
        End Sub

        Private Shared Function GetLayerName(LayerId As Integer) As String
            Select Case LayerId
                Case 0
                    Return "Skin"
                Case 1
                    Return "Hair"
                Case 2
                    Return "Metal 1"
                Case 3
                    Return "Metal 2"
                Case 4
                    Return "Cloth 1"
                Case 5
                    Return "Cloth 2"
                Case 6
                    Return "Leather 1"
                Case 7
                    Return "Leather 2"
                Case 8
                    Return "Tattoo 1"
                Case 9
                    Return "Tattoo 2"
            End Select

            Return "Layer"
        End Function

        Private Shared Function CheckedToInteger(Value As Long, ErrorMessage As String) As Integer
            If Value < 0L OrElse Value > Integer.MaxValue Then Throw New InvalidDataException(ErrorMessage)
            Return CInt(Value)
        End Function

    End Class

End Module
