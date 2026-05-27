Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO

Module ICOView

    Public NotInheritable Class IcoFile

        Private Structure IcoEntry
            Public WidthByte As Byte
            Public HeightByte As Byte
            Public ColorCount As Byte
            Public Reserved As Byte
            Public Planes As UShort
            Public BitCount As UShort
            Public BytesInResource As UInteger
            Public ImageOffset As UInteger
            Public Width As Integer
            Public Height As Integer
        End Structure

        Private ReadOnly Data As Byte()
        Private ReadOnly Entries As New List(Of IcoEntry)

        Public Sub New(SourceStream As Stream)
            If SourceStream Is Nothing Then Throw New ArgumentNullException("SourceStream")
            If SourceStream.Length < 6 Then Throw New InvalidDataException("ICO file is too small.")

            Using memory As New MemoryStream()
                SourceStream.CopyTo(memory)
                Data = memory.ToArray()
            End Using

            ParseHeader()
        End Sub

        Public ReadOnly Property ImageCount As Integer
            Get
                Return Entries.Count
            End Get
        End Property

        Public Function GetBitmap(ImageIndex As Integer) As Bitmap
            ImageIndex = ClampImageIndex(ImageIndex)
            Dim iconEntry As IcoEntry = Entries(ImageIndex)
            Dim imageData As Byte() = GetImageData(iconEntry)

            If IsPngImage(imageData) Then
                Using memory As New MemoryStream(imageData, False)
                    Using image As Image = Image.FromStream(memory, True, True)
                        Return CloneAs32BppArgb(image)
                    End Using
                End Using
            End If

            Using singleIconStream As MemoryStream = CreateSingleIconStream(iconEntry, imageData)
                Using iconImage As New Icon(singleIconStream)
                    Using iconBitmap As Bitmap = iconImage.ToBitmap()
                        Return CloneAs32BppArgb(iconBitmap)
                    End Using
                End Using
            End Using
        End Function

        Public Function GetBestBitmap(Optional MaxPixelSize As Integer = 0) As Bitmap
            Return GetBitmap(GetBestImageIndex(MaxPixelSize))
        End Function

        Public Function GetBestImageIndex(Optional MaxPixelSize As Integer = 0) As Integer
            If Entries.Count = 0 Then Throw New InvalidDataException("ICO file does not contain any images.")

            Dim bestIndex As Integer = -1

            If MaxPixelSize > 0 Then
                For i As Integer = 0 To Entries.Count - 1
                    If Entries(i).Width <= MaxPixelSize AndAlso Entries(i).Height <= MaxPixelSize Then
                        If bestIndex < 0 OrElse IsLargerOrSharper(Entries(i), Entries(bestIndex)) Then bestIndex = i
                    End If
                Next

                If bestIndex >= 0 Then Return bestIndex

                For i As Integer = 0 To Entries.Count - 1
                    If bestIndex < 0 OrElse IsSmallerOrSharper(Entries(i), Entries(bestIndex)) Then bestIndex = i
                Next

                Return bestIndex
            End If

            For i As Integer = 0 To Entries.Count - 1
                If bestIndex < 0 OrElse IsLargerOrSharper(Entries(i), Entries(bestIndex)) Then bestIndex = i
            Next

            Return bestIndex
        End Function

        Public Function GetIconWidth(ImageIndex As Integer) As Integer
            Return Entries(ClampImageIndex(ImageIndex)).Width
        End Function

        Public Function GetIconHeight(ImageIndex As Integer) As Integer
            Return Entries(ClampImageIndex(ImageIndex)).Height
        End Function

        Public Function GetIconImageSize(ImageIndex As Integer) As UInteger
            Return Entries(ClampImageIndex(ImageIndex)).BytesInResource
        End Function

        Public Function GetIconImageOffset(ImageIndex As Integer) As UInteger
            Return Entries(ClampImageIndex(ImageIndex)).ImageOffset
        End Function

        Public Function GetBitsPerPixel(ImageIndex As Integer) As Integer
            Return Entries(ClampImageIndex(ImageIndex)).BitCount
        End Function

        Public Function GetFormatName(ImageIndex As Integer) As String
            Dim iconEntry As IcoEntry = Entries(ClampImageIndex(ImageIndex))
            If IsPngIconImage(iconEntry) Then Return "Embedded PNG"
            Return "DIB bitmap"
        End Function

        Public Function GetAlphaChannelText(ImageIndex As Integer) As String
            Dim iconEntry As IcoEntry = Entries(ClampImageIndex(ImageIndex))

            If iconEntry.BitCount >= 32 Then Return "8 bits"
            If IsPngIconImage(iconEntry) Then Return "PNG alpha"
            If iconEntry.BitCount = 0 Then Return "Unknown"
            Return "1 bit mask"
        End Function

        Private Sub ParseHeader()
            Dim reserved As UShort = ReadUInt16(0)
            Dim iconType As UShort = ReadUInt16(2)
            Dim count As UShort = ReadUInt16(4)

            If reserved <> 0US OrElse iconType <> 1US Then Throw New InvalidDataException("Unsupported ICO header.")
            If count = 0US Then Throw New InvalidDataException("ICO file does not contain any images.")
            If count > 1024US Then Throw New InvalidDataException("ICO image count is unreasonable.")

            Dim directoryLength As Long = 6L + CLng(count) * 16L
            If directoryLength > Data.Length Then Throw New InvalidDataException("ICO directory is incomplete.")

            For i As Integer = 0 To count - 1
                Dim entryOffset As Integer = 6 + i * 16
                Dim iconEntry As New IcoEntry()

                iconEntry.WidthByte = Data(entryOffset)
                iconEntry.HeightByte = Data(entryOffset + 1)
                iconEntry.ColorCount = Data(entryOffset + 2)
                iconEntry.Reserved = Data(entryOffset + 3)
                iconEntry.Planes = ReadUInt16(entryOffset + 4)
                iconEntry.BitCount = ReadUInt16(entryOffset + 6)
                iconEntry.BytesInResource = ReadUInt32(entryOffset + 8)
                iconEntry.ImageOffset = ReadUInt32(entryOffset + 12)
                iconEntry.Width = If(iconEntry.WidthByte = 0, 256, CInt(iconEntry.WidthByte))
                iconEntry.Height = If(iconEntry.HeightByte = 0, 256, CInt(iconEntry.HeightByte))

                ValidateEntry(iconEntry, i, directoryLength)
                Entries.Add(iconEntry)
            Next
        End Sub

        Private Sub ValidateEntry(iconEntry As IcoEntry, ImageIndex As Integer, DirectoryLength As Long)
            If iconEntry.Reserved <> 0 Then Throw New InvalidDataException("ICO directory entry " & ImageIndex.ToString() & " is invalid.")
            If iconEntry.Width <= 0 OrElse iconEntry.Height <= 0 Then Throw New InvalidDataException("ICO image dimensions are invalid.")
            If iconEntry.BytesInResource = 0UI Then Throw New InvalidDataException("ICO image " & ImageIndex.ToString() & " is empty.")
            If iconEntry.ImageOffset > Integer.MaxValue OrElse iconEntry.BytesInResource > Integer.MaxValue Then Throw New InvalidDataException("ICO image " & ImageIndex.ToString() & " is too large.")
            If CULng(iconEntry.ImageOffset) < CULng(DirectoryLength) Then Throw New InvalidDataException("ICO image " & ImageIndex.ToString() & " overlaps the icon directory.")

            Dim imageEnd As ULong = CULng(iconEntry.ImageOffset) + CULng(iconEntry.BytesInResource)
            If imageEnd > CULng(Data.Length) Then Throw New InvalidDataException("ICO image " & ImageIndex.ToString() & " points past the end of the file.")
        End Sub

        Private Function GetImageData(iconEntry As IcoEntry) As Byte()
            Dim imageData As Byte() = New Byte(CInt(iconEntry.BytesInResource) - 1) {}
            Buffer.BlockCopy(Data, CInt(iconEntry.ImageOffset), imageData, 0, imageData.Length)
            Return imageData
        End Function

        Private Function CreateSingleIconStream(iconEntry As IcoEntry, imageData As Byte()) As MemoryStream
            Dim memory As New MemoryStream()

            Using writer As New BinaryWriter(memory, System.Text.Encoding.Default, True)
                writer.Write(CUShort(0))
                writer.Write(CUShort(1))
                writer.Write(CUShort(1))
                writer.Write(iconEntry.WidthByte)
                writer.Write(iconEntry.HeightByte)
                writer.Write(iconEntry.ColorCount)
                writer.Write(iconEntry.Reserved)
                writer.Write(iconEntry.Planes)
                writer.Write(iconEntry.BitCount)
                writer.Write(CUInt(imageData.Length))
                writer.Write(CUInt(22))
                writer.Write(imageData)
            End Using

            memory.Position = 0
            Return memory
        End Function

        Private Function ClampImageIndex(ImageIndex As Integer) As Integer
            If Entries.Count = 0 Then Throw New InvalidDataException("ICO file does not contain any images.")
            If ImageIndex < 0 Then Return 0
            If ImageIndex >= Entries.Count Then Return Entries.Count - 1
            Return ImageIndex
        End Function

        Private Shared Function IsLargerOrSharper(candidate As IcoEntry, current As IcoEntry) As Boolean
            Dim candidateArea As Long = CLng(candidate.Width) * CLng(candidate.Height)
            Dim currentArea As Long = CLng(current.Width) * CLng(current.Height)

            If candidateArea <> currentArea Then Return candidateArea > currentArea
            Return candidate.BitCount > current.BitCount
        End Function

        Private Shared Function IsSmallerOrSharper(candidate As IcoEntry, current As IcoEntry) As Boolean
            Dim candidateArea As Long = CLng(candidate.Width) * CLng(candidate.Height)
            Dim currentArea As Long = CLng(current.Width) * CLng(current.Height)

            If candidateArea <> currentArea Then Return candidateArea < currentArea
            Return candidate.BitCount > current.BitCount
        End Function

        Private Function IsPngIconImage(iconEntry As IcoEntry) As Boolean
            Dim imageData As Byte() = GetImageData(iconEntry)
            Return IsPngImage(imageData)
        End Function

        Private Shared Function IsPngImage(imageData As Byte()) As Boolean
            Return imageData IsNot Nothing AndAlso imageData.Length >= 8 AndAlso
                   imageData(0) = &H89 AndAlso
                   imageData(1) = AscW("P"c) AndAlso
                   imageData(2) = AscW("N"c) AndAlso
                   imageData(3) = AscW("G"c) AndAlso
                   imageData(4) = &HD AndAlso
                   imageData(5) = &HA AndAlso
                   imageData(6) = &H1A AndAlso
                   imageData(7) = &HA
        End Function

        Private Shared Function CloneAs32BppArgb(Source As Image) As Bitmap
            Dim clone As New Bitmap(Source.Width, Source.Height, PixelFormat.Format32bppArgb)
            Using graphics As Graphics = Graphics.FromImage(clone)
                graphics.CompositingMode = CompositingMode.SourceCopy
                graphics.DrawImage(Source, New Rectangle(0, 0, Source.Width, Source.Height))
            End Using
            Return clone
        End Function

        Private Function ReadUInt16(Offset As Integer) As UShort
            Return BitConverter.ToUInt16(Data, Offset)
        End Function

        Private Function ReadUInt32(Offset As Integer) As UInteger
            Return BitConverter.ToUInt32(Data, Offset)
        End Function

    End Class

End Module
