Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text

Module TextureResizeWriters

    Public Enum TextureResizeOutputFormat
        Blp
        Dds
        Tga
        Ico
        Png
        Jpeg
    End Enum

    Public Sub SaveResizedBitmap(Source As Bitmap,
                                 OutputPath As String,
                                 OutputFormat As TextureResizeOutputFormat,
                                 NewWidth As Integer,
                                 NewHeight As Integer,
                                 JpegBackgroundColor As Color)
        If Source Is Nothing Then Throw New ArgumentNullException("Source")
        If String.IsNullOrWhiteSpace(OutputPath) Then Throw New ArgumentException("Output path is required.", "OutputPath")
        ValidateResizeDimensions(OutputFormat, NewWidth, NewHeight)

        Using resized As Bitmap = CreateResizedBitmap(Source, NewWidth, NewHeight)
            Select Case OutputFormat
                Case TextureResizeOutputFormat.Blp
                    SaveBitmapAsBlp(resized, OutputPath)
                Case TextureResizeOutputFormat.Dds
                    SaveBitmapAsDds(resized, OutputPath)
                Case TextureResizeOutputFormat.Tga
                    SaveBitmapAsTga(resized, OutputPath)
                Case TextureResizeOutputFormat.Ico
                    SaveBitmapAsIco(resized, OutputPath)
                Case TextureResizeOutputFormat.Png
                    resized.Save(OutputPath, ImageFormat.Png)
                Case TextureResizeOutputFormat.Jpeg
                    SaveBitmapAsJpeg(resized, OutputPath, JpegBackgroundColor)
                Case Else
                    Throw New InvalidDataException("Unsupported resize output format.")
            End Select
        End Using
    End Sub

    Public Sub ValidateResizeDimensions(OutputFormat As TextureResizeOutputFormat, NewWidth As Integer, NewHeight As Integer)
        If NewWidth <= 0 OrElse NewHeight <= 0 Then Throw New InvalidDataException("Texture dimensions must be greater than zero.")

        Select Case OutputFormat
            Case TextureResizeOutputFormat.Ico
                If NewWidth > 256 OrElse NewHeight > 256 Then Throw New InvalidDataException("ICO dimensions cannot be larger than 256 x 256.")
            Case TextureResizeOutputFormat.Tga
                If NewWidth > UShort.MaxValue OrElse NewHeight > UShort.MaxValue Then Throw New InvalidDataException("TGA dimensions cannot be larger than 65535 x 65535.")
        End Select

        Dim pixelBytes As Long = CLng(NewWidth) * CLng(NewHeight) * 4L
        If pixelBytes <= 0L OrElse pixelBytes > Integer.MaxValue Then Throw New InvalidDataException("The requested texture resolution is too large to save safely.")
    End Sub

    Private Function CreateResizedBitmap(Source As Bitmap, NewWidth As Integer, NewHeight As Integer) As Bitmap
        Dim resized As New Bitmap(NewWidth, NewHeight, PixelFormat.Format32bppArgb)

        Using graphics As Graphics = Graphics.FromImage(resized)
            graphics.CompositingMode = CompositingMode.SourceCopy
            graphics.CompositingQuality = CompositingQuality.HighQuality
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality
            graphics.SmoothingMode = SmoothingMode.HighQuality
            graphics.DrawImage(Source, New Rectangle(0, 0, NewWidth, NewHeight), 0, 0, Source.Width, Source.Height, GraphicsUnit.Pixel)
        End Using

        Return resized
    End Function

    Private Sub SaveBitmapAsJpeg(Source As Bitmap, OutputPath As String, BackgroundColor As Color)
        Using flattened As New Bitmap(Source.Width, Source.Height, PixelFormat.Format24bppRgb)
            Using graphics As Graphics = Graphics.FromImage(flattened)
                graphics.Clear(BackgroundColor)
                graphics.CompositingMode = CompositingMode.SourceOver
                graphics.DrawImage(Source, New Rectangle(0, 0, Source.Width, Source.Height), 0, 0, Source.Width, Source.Height, GraphicsUnit.Pixel)
            End Using

            flattened.Save(OutputPath, ImageFormat.Jpeg)
        End Using
    End Sub

    Private Sub SaveBitmapAsTga(Source As Bitmap, OutputPath As String)
        Dim bgra As Byte() = GetBgraBytesTopDown(Source)

        Using fileStream As New FileStream(OutputPath, FileMode.Create, FileAccess.Write, FileShare.None)
            Using writer As New BinaryWriter(fileStream, Encoding.ASCII)
                writer.Write(CByte(0))   ' ID length
                writer.Write(CByte(0))   ' Color map type
                writer.Write(CByte(2))   ' Uncompressed true-color image
                writer.Write(CUShort(0)) ' Color map first entry
                writer.Write(CUShort(0)) ' Color map length
                writer.Write(CByte(0))   ' Color map entry size
                writer.Write(CUShort(0)) ' X origin
                writer.Write(CUShort(0)) ' Y origin
                writer.Write(CUShort(Source.Width))
                writer.Write(CUShort(Source.Height))
                writer.Write(CByte(32))  ' BGRA
                writer.Write(CByte(&H28)) ' Top-left origin, 8 alpha bits
                writer.Write(bgra)
            End Using
        End Using
    End Sub

    Private Sub SaveBitmapAsIco(Source As Bitmap, OutputPath As String)
        Dim pngBytes As Byte()
        Using memory As New MemoryStream()
            Source.Save(memory, ImageFormat.Png)
            pngBytes = memory.ToArray()
        End Using

        Using fileStream As New FileStream(OutputPath, FileMode.Create, FileAccess.Write, FileShare.None)
            Using writer As New BinaryWriter(fileStream, Encoding.ASCII)
                writer.Write(CUShort(0))
                writer.Write(CUShort(1))
                writer.Write(CUShort(1))
                writer.Write(IconDimensionByte(Source.Width))
                writer.Write(IconDimensionByte(Source.Height))
                writer.Write(CByte(0))
                writer.Write(CByte(0))
                writer.Write(CUShort(1))
                writer.Write(CUShort(32))
                writer.Write(CUInt(pngBytes.Length))
                writer.Write(CUInt(22))
                writer.Write(pngBytes)
            End Using
        End Using
    End Sub

    Private Sub SaveBitmapAsDds(Source As Bitmap, OutputPath As String)
        Dim bgra As Byte() = GetBgraBytesTopDown(Source)
        Dim pitch As UInteger = CUInt(Source.Width * 4)

        Using fileStream As New FileStream(OutputPath, FileMode.Create, FileAccess.Write, FileShare.None)
            Using writer As New BinaryWriter(fileStream, Encoding.ASCII)
                writer.Write(Encoding.ASCII.GetBytes("DDS "))
                writer.Write(CUInt(124))
                writer.Write(CUInt(&H100F)) ' CAPS | HEIGHT | WIDTH | PITCH | PIXELFORMAT
                writer.Write(CUInt(Source.Height))
                writer.Write(CUInt(Source.Width))
                writer.Write(pitch)
                writer.Write(CUInt(0))
                writer.Write(CUInt(0))
                For i As Integer = 0 To 10
                    writer.Write(CUInt(0))
                Next

                writer.Write(CUInt(32))
                writer.Write(CUInt(&H41)) ' RGB | ALPHAPIXELS
                writer.Write(CUInt(0))
                writer.Write(CUInt(32))
                writer.Write(CUInt(&HFF0000))
                writer.Write(CUInt(&HFF00))
                writer.Write(CUInt(&HFF))
                writer.Write(CUInt(&HFF000000UI))
                writer.Write(CUInt(&H1000)) ' DDSCAPS_TEXTURE
                writer.Write(CUInt(0))
                writer.Write(CUInt(0))
                writer.Write(CUInt(0))
                writer.Write(CUInt(0))
                writer.Write(bgra)
            End Using
        End Using
    End Sub

    Private Sub SaveBitmapAsBlp(Source As Bitmap, OutputPath As String)
        Dim bgra As Byte() = GetBgraBytesTopDown(Source)
        Dim mipOffset As UInteger = 148UI
        Dim mipSize As UInteger = CUInt(bgra.Length)

        Using fileStream As New FileStream(OutputPath, FileMode.Create, FileAccess.Write, FileShare.None)
            Using writer As New BinaryWriter(fileStream, Encoding.ASCII)
                writer.Write(Encoding.ASCII.GetBytes("BLP2"))
                writer.Write(CUInt(1))      ' BLP2 texture type
                writer.Write(CByte(3))      ' RAW BGRA
                writer.Write(CByte(8))      ' Alpha depth
                writer.Write(CByte(0))      ' Alpha encoding
                writer.Write(CByte(0))      ' No mipmaps
                writer.Write(CInt(Source.Width))
                writer.Write(CInt(Source.Height))

                writer.Write(mipOffset)
                For i As Integer = 1 To 15
                    writer.Write(CUInt(0))
                Next

                writer.Write(mipSize)
                For i As Integer = 1 To 15
                    writer.Write(CUInt(0))
                Next

                writer.Write(bgra)
            End Using
        End Using
    End Sub

    Private Function GetBgraBytesTopDown(Source As Bitmap) As Byte()
        Dim rect As New Rectangle(0, 0, Source.Width, Source.Height)
        Dim result As Byte() = New Byte(Source.Width * Source.Height * 4 - 1) {}
        Dim bitmapData As BitmapData = Source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)

        Try
            Dim rowBytes As Integer = Source.Width * 4
            Dim rowBuffer As Byte() = New Byte(rowBytes - 1) {}

            For y As Integer = 0 To Source.Height - 1
                Dim sourceRow As IntPtr = IntPtr.Add(bitmapData.Scan0, y * bitmapData.Stride)
                Marshal.Copy(sourceRow, rowBuffer, 0, rowBytes)
                Buffer.BlockCopy(rowBuffer, 0, result, y * rowBytes, rowBytes)
            Next
        Finally
            Source.UnlockBits(bitmapData)
        End Try

        Return result
    End Function

    Private Function IconDimensionByte(Value As Integer) As Byte
        If Value = 256 Then Return 0
        Return CByte(Value)
    End Function

End Module
