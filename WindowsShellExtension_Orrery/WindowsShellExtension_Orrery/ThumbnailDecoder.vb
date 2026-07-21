Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO

Friend NotInheritable Class ThumbnailDecoder

    Private Const MaxDecodedPixelCount As Long = 16777216L

    Private Sub New()
    End Sub

    Public Shared Function Decode(FileData As Byte(), MaxPixelSize As Integer) As Bitmap
        If FileData Is Nothing OrElse FileData.Length = 0 Then Throw New InvalidDataException("The thumbnail stream is empty.")
        If MaxPixelSize <= 0 Then MaxPixelSize = 256

        Dim decoded As Bitmap = Nothing

        If IsBlp(FileData) Then
            decoded = DecodeBlp(FileData, MaxPixelSize)
        ElseIf IsPlt(FileData) Then
            decoded = DecodePlt(FileData)
        ElseIf IsIco(FileData) Then
            decoded = DecodeIco(FileData, MaxPixelSize)
        Else
            decoded = DecodeDds(FileData, MaxPixelSize)
        End If

        Try
            Return ScaleToFit(decoded, MaxPixelSize)
        Finally
            decoded.Dispose()
        End Try
    End Function

    Private Shared Function DecodeBlp(FileData As Byte(), MaxPixelSize As Integer) As Bitmap
        Using memory As New MemoryStream(FileData, False)
            Using blp As New BlpFile(memory)
                If Not blp.GetIsValidVersion() Then Throw New InvalidDataException("Unsupported BLP file.")
                Dim mip As Integer = ChooseBestReadableBlpMipmap(blp, MaxPixelSize)
                If mip < 0 Then Throw New InvalidDataException("BLP file does not contain a readable mipmap.")
                EnsureSafeDecodeDimensions(blp.GetMipmapWidth(mip), blp.GetMipmapHeight(mip), "BLP")
                Return blp.GetBitmap(mip)
            End Using
        End Using
    End Function

    Private Shared Function DecodePlt(FileData As Byte()) As Bitmap
        Using memory As New MemoryStream(FileData, False)
            Dim plt As New PltFile(memory)
            EnsureSafeDecodeDimensions(plt.GetPltWidth(), plt.GetPltHeight(), "PLT")
            Return plt.GetBitmap()
        End Using
    End Function

    Private Shared Function DecodeDds(FileData As Byte(), MaxPixelSize As Integer) As Bitmap
        Using memory As New MemoryStream(FileData, False)
            Dim dds As New DdsFile(memory)
            Dim mip As Integer = ChooseBestMipmap(dds.MipMapCount,
                                                   Function(i) dds.GetMipmapWidth(i),
                                                   Function(i) dds.GetMipmapHeight(i),
                                                   MaxPixelSize)
            EnsureSafeDecodeDimensions(dds.GetMipmapWidth(mip), dds.GetMipmapHeight(mip), "DDS")
            Dim bitmap As Bitmap = dds.GetBitmap(mip)
            If dds.GetIsBioWareCompact() Then bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY)
            Return bitmap
        End Using
    End Function

    Private Shared Function DecodeIco(FileData As Byte(), MaxPixelSize As Integer) As Bitmap
        Using memory As New MemoryStream(FileData, False)
            Dim ico As New IcoFile(memory)
            Return ico.GetBestBitmap(MaxPixelSize)
        End Using
    End Function

    Private Shared Function ChooseBestMipmap(MipmapCount As Integer,
                                             GetWidth As Func(Of Integer, Integer),
                                             GetHeight As Func(Of Integer, Integer),
                                             MaxPixelSize As Integer) As Integer
        If MipmapCount <= 1 Then Return 0

        For i As Integer = 0 To MipmapCount - 1
            If GetWidth(i) <= MaxPixelSize AndAlso GetHeight(i) <= MaxPixelSize Then Return i
        Next

        Return MipmapCount - 1
    End Function

    Private Shared Function ChooseBestReadableBlpMipmap(Blp As BlpFile, MaxPixelSize As Integer) As Integer
        If Blp Is Nothing OrElse Blp.MipMapCount <= 0 Then Return -1

        Dim fallback As Integer = -1

        For i As Integer = 0 To Blp.MipMapCount - 1
            If Not Blp.IsMipmapReadable(i) Then Continue For
            fallback = i
            If Blp.GetMipmapWidth(i) <= MaxPixelSize AndAlso Blp.GetMipmapHeight(i) <= MaxPixelSize Then Return i
        Next

        Return fallback
    End Function

    Private Shared Sub EnsureSafeDecodeDimensions(Width As Integer, Height As Integer, FormatName As String)
        If Width <= 0 OrElse Height <= 0 Then Throw New InvalidDataException(FormatName & " thumbnail dimensions are invalid.")

        Dim pixelCount As Long = CLng(Width) * CLng(Height)
        If pixelCount <= 0L OrElse pixelCount > MaxDecodedPixelCount Then
            Throw New InvalidDataException(FormatName & " thumbnail source is too large to decode safely inside Explorer.")
        End If
    End Sub

    Private Shared Function ScaleToFit(Source As Bitmap, MaxPixelSize As Integer) As Bitmap
        If Source.Width <= MaxPixelSize AndAlso Source.Height <= MaxPixelSize Then
            Return CloneAsArgb(Source)
        End If

        Dim scale As Double = Math.Min(MaxPixelSize / CDbl(Source.Width), MaxPixelSize / CDbl(Source.Height))
        Dim targetWidth As Integer = Math.Max(1, CInt(Math.Round(Source.Width * scale)))
        Dim targetHeight As Integer = Math.Max(1, CInt(Math.Round(Source.Height * scale)))

        Dim thumbnail As New Bitmap(targetWidth, targetHeight, PixelFormat.Format32bppArgb)
        Using graphics As Graphics = Graphics.FromImage(thumbnail)
            graphics.CompositingMode = CompositingMode.SourceCopy
            graphics.CompositingQuality = CompositingQuality.HighQuality
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality
            graphics.SmoothingMode = SmoothingMode.HighQuality
            graphics.DrawImage(Source, New Rectangle(0, 0, targetWidth, targetHeight))
        End Using

        Return thumbnail
    End Function

    Private Shared Function CloneAsArgb(Source As Bitmap) As Bitmap
        Dim clone As New Bitmap(Source.Width, Source.Height, PixelFormat.Format32bppArgb)
        Using graphics As Graphics = Graphics.FromImage(clone)
            graphics.CompositingMode = CompositingMode.SourceCopy
            graphics.DrawImage(Source, New Rectangle(0, 0, Source.Width, Source.Height))
        End Using
        Return clone
    End Function

    Private Shared Function IsBlp(FileData As Byte()) As Boolean
        Return FileData.Length >= 4 AndAlso
               FileData(0) = AscW("B"c) AndAlso
               FileData(1) = AscW("L"c) AndAlso
               FileData(2) = AscW("P"c) AndAlso
               FileData(3) = AscW("2"c)
    End Function

    Private Shared Function IsPlt(FileData As Byte()) As Boolean
        Return FileData.Length >= 24 AndAlso
               FileData(0) = AscW("P"c) AndAlso
               FileData(1) = AscW("L"c) AndAlso
               FileData(2) = AscW("T"c)
    End Function

    Private Shared Function IsIco(FileData As Byte()) As Boolean
        If FileData.Length < 6 Then Return False

        Dim reserved As UShort = BitConverter.ToUInt16(FileData, 0)
        Dim iconType As UShort = BitConverter.ToUInt16(FileData, 2)
        Dim count As UShort = BitConverter.ToUInt16(FileData, 4)

        If reserved <> 0US OrElse iconType <> 1US OrElse count = 0US OrElse count > 1024US Then Return False

        Dim directoryLength As Long = 6L + CLng(count) * 16L
        If directoryLength > FileData.Length Then Return False

        For i As Integer = 0 To count - 1
            Dim entryOffset As Integer = 6 + i * 16
            Dim entryReserved As Byte = FileData(entryOffset + 3)
            Dim bytesInResource As UInteger = BitConverter.ToUInt32(FileData, entryOffset + 8)
            Dim imageOffset As UInteger = BitConverter.ToUInt32(FileData, entryOffset + 12)
            Dim imageEnd As ULong = CULng(imageOffset) + CULng(bytesInResource)

            If entryReserved <> 0 Then Return False
            If bytesInResource = 0UI Then Return False
            If CULng(imageOffset) < CULng(directoryLength) Then Return False
            If imageEnd > CULng(FileData.Length) Then Return False
        Next

        Return True
    End Function

End Class
