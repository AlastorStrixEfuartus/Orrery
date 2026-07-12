Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text

Public Module DDSView

    Public NotInheritable Class DdsFile

        Private Enum DdsContainer
            Standard
            BioWareCompact
        End Enum

        Private Enum DdsPixelStorage
            Dxt1
            Dxt3
            Dxt5
            Rgba
        End Enum

        Private Structure DdsMipLevel
            Public Width As Integer
            Public Height As Integer
            Public Offset As Integer
            Public Size As Integer
        End Structure

        Private Const DDPF_ALPHAPIXELS As UInteger = &H1UI
        Private Const DDPF_FOURCC As UInteger = &H4UI
        Private Const DDPF_RGB As UInteger = &H40UI
        Private Const DDPF_LUMINANCE As UInteger = &H20000UI
        Private Const DDSD_PITCH As UInteger = &H8UI

        Private ReadOnly Data As Byte()
        Private ReadOnly MipLevels As New List(Of DdsMipLevel)
        Private Container As DdsContainer
        Private PixelStorage As DdsPixelStorage
        Private HeaderFlags As UInteger
        Private Width As Integer
        Private Height As Integer
        Private PixelFlags As UInteger
        Private FourCC As String = String.Empty
        Private RgbBitCount As Integer
        Private RBitMask As UInteger
        Private GBitMask As UInteger
        Private BBitMask As UInteger
        Private ABitMask As UInteger
        Private AlphaMean As Single
        Private IsDx10 As Boolean
        Private DxgiFormat As UInteger

        Public Sub New(SourceStream As Stream)
            If SourceStream Is Nothing Then Throw New ArgumentNullException("SourceStream")
            If SourceStream.Length < 20 Then Throw New InvalidDataException("DDS file is too small.")

            Using memory As New MemoryStream()
                SourceStream.CopyTo(memory)
                Data = memory.ToArray()
            End Using

            If HasStandardMagic() Then
                ParseStandardDds()
            Else
                ParseBioWareCompactDds()
            End If
        End Sub

        Public ReadOnly Property MipMapCount As Integer
            Get
                Return MipLevels.Count
            End Get
        End Property

        Public Function GetBitmap(MipmapLevel As Integer, Optional NormalizeBioWareOrientation As Boolean = True) As Bitmap
            MipmapLevel = ClampMipmapLevel(MipmapLevel)

            Dim mip As DdsMipLevel = MipLevels(MipmapLevel)
            Dim rgba As Byte() = GetImageBytes(MipmapLevel)
            ConvertRgbaToBgra(rgba)

            Dim bmp As New Bitmap(mip.Width, mip.Height, Imaging.PixelFormat.Format32bppArgb)
            Dim bmpData As Imaging.BitmapData = bmp.LockBits(New Rectangle(0, 0, mip.Width, mip.Height),
                                                              Imaging.ImageLockMode.WriteOnly,
                                                              Imaging.PixelFormat.Format32bppArgb)
            Try
                Marshal.Copy(rgba, 0, bmpData.Scan0, rgba.Length)
            Finally
                bmp.UnlockBits(bmpData)
            End Try

            If NormalizeBioWareOrientation AndAlso Container = DdsContainer.BioWareCompact Then
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY)
            End If

            Return bmp
        End Function

        Public Function GetImageBytes(MipmapLevel As Integer) As Byte()
            MipmapLevel = ClampMipmapLevel(MipmapLevel)
            Dim mip As DdsMipLevel = MipLevels(MipmapLevel)
            Dim bytes As Byte() = ReadBytes(mip.Offset, mip.Size)

            Select Case PixelStorage
                Case DdsPixelStorage.Dxt1, DdsPixelStorage.Dxt3, DdsPixelStorage.Dxt5
                    Dim rgba As Byte() = New Byte(mip.Width * mip.Height * 4 - 1) {}
                    DecompressImage(rgba, mip.Width, mip.Height, bytes, GetDxtFlags())
                    Return rgba
                Case DdsPixelStorage.Rgba
                    Return DecodeRawPixels(bytes, mip.Width, mip.Height, MipmapLevel)
            End Select

            Throw New InvalidDataException("Unsupported DDS pixel storage.")
        End Function

        Public Function GetMipmapWidth(MipmapLevel As Integer) As Integer
            Return MipLevels(ClampMipmapLevel(MipmapLevel)).Width
        End Function

        Public Function GetMipmapHeight(MipmapLevel As Integer) As Integer
            Return MipLevels(ClampMipmapLevel(MipmapLevel)).Height
        End Function

        Public Function GetMipMapOffset(MipmapLevel As Integer) As Integer
            Return MipLevels(ClampMipmapLevel(MipmapLevel)).Offset
        End Function

        Public Function GetMipMapSize(MipmapLevel As Integer) As Integer
            Return MipLevels(ClampMipmapLevel(MipmapLevel)).Size
        End Function

        Public Function GetContainerName() As String
            If Container = DdsContainer.BioWareCompact Then Return "BioWare DDS"
            If IsDx10 Then Return "DDS DX10"
            Return "DDS"
        End Function

        Public Function GetIsBioWareCompact() As Boolean
            Return Container = DdsContainer.BioWareCompact
        End Function

        Public Function GetFormatName() As String
            Select Case PixelStorage
                Case DdsPixelStorage.Dxt1
                    Return "DXT1"
                Case DdsPixelStorage.Dxt3
                    Return "DXT3"
                Case DdsPixelStorage.Dxt5
                    Return "DXT5"
                Case DdsPixelStorage.Rgba
                    Return If(RgbBitCount > 0, "RGB/RGBA " & RgbBitCount.ToString() & "-bit", "RGBA")
            End Select

            Return "Unknown"
        End Function

        Public Function GetAlphaChannelText() As String
            Select Case PixelStorage
                Case DdsPixelStorage.Dxt1
                    If (GetDxtFlags() And DXTFlags.DXT1Alpha) <> 0 Then Return "1 bit"
                    Return "None"
                Case DdsPixelStorage.Dxt3
                    Return "4 bits"
                Case DdsPixelStorage.Dxt5
                    Return "8 bits"
                Case DdsPixelStorage.Rgba
                    If ABitMask <> 0 Then Return CountMaskBits(ABitMask).ToString() & " bits"
                    Return "None"
            End Select

            Return "N/A"
        End Function

        Private Function HasStandardMagic() As Boolean
            Return Data.Length >= 4 AndAlso Data(0) = AscW("D"c) AndAlso Data(1) = AscW("D"c) AndAlso Data(2) = AscW("S"c) AndAlso Data(3) = AscW(" "c)
        End Function

        Private Sub ParseStandardDds()
            If Data.Length < 128 Then Throw New InvalidDataException("Standard DDS header is incomplete.")

            Container = DdsContainer.Standard

            Dim headerSize As UInteger = ReadUInt32(4)
            If headerSize <> 124UI Then Throw New InvalidDataException("Unsupported DDS header size: " & headerSize.ToString())

            HeaderFlags = ReadUInt32(8)
            Height = CheckedPositiveDimension(ReadUInt32(12), "height")
            Width = CheckedPositiveDimension(ReadUInt32(16), "width")
            Dim pitchOrLinearSize As UInteger = ReadUInt32(20)
            Dim declaredMipCount As Integer = CInt(Math.Max(1UI, ReadUInt32(28)))

            Dim pixelFormatOffset As Integer = 76
            Dim pixelFormatSize As UInteger = ReadUInt32(pixelFormatOffset)
            If pixelFormatSize <> 32UI Then Throw New InvalidDataException("Unsupported DDS pixel format header size: " & pixelFormatSize.ToString())

            PixelFlags = ReadUInt32(pixelFormatOffset + 4)
            FourCC = Encoding.ASCII.GetString(Data, pixelFormatOffset + 8, 4)
            RgbBitCount = CInt(ReadUInt32(pixelFormatOffset + 12))
            RBitMask = ReadUInt32(pixelFormatOffset + 16)
            GBitMask = ReadUInt32(pixelFormatOffset + 20)
            BBitMask = ReadUInt32(pixelFormatOffset + 24)
            ABitMask = ReadUInt32(pixelFormatOffset + 28)

            Dim dataOffset As Integer = 128

            If (PixelFlags And DDPF_FOURCC) <> 0 Then
                Select Case FourCC
                    Case "DXT1"
                        PixelStorage = DdsPixelStorage.Dxt1
                    Case "DXT2", "DXT3"
                        PixelStorage = DdsPixelStorage.Dxt3
                    Case "DXT4", "DXT5"
                        PixelStorage = DdsPixelStorage.Dxt5
                    Case "DX10"
                        If Data.Length < 148 Then Throw New InvalidDataException("DDS DX10 header is incomplete.")
                        IsDx10 = True
                        DxgiFormat = ReadUInt32(128)
                        PixelStorage = GetDx10PixelStorage(DxgiFormat)
                        dataOffset += 20
                    Case Else
                        Throw New InvalidDataException("Unsupported DDS FourCC: " & FourCC)
                End Select
            ElseIf (PixelFlags And (DDPF_RGB Or DDPF_LUMINANCE Or DDPF_ALPHAPIXELS)) <> 0 Then
                PixelStorage = DdsPixelStorage.Rgba
                If RgbBitCount <= 0 Then Throw New InvalidDataException("DDS raw RGB bit count is missing.")
            Else
                Throw New InvalidDataException("Unsupported DDS pixel format flags.")
            End If

            BuildMipLevels(dataOffset, declaredMipCount, CInt(pitchOrLinearSize))
        End Sub

        Private Sub ParseBioWareCompactDds()
            Container = DdsContainer.BioWareCompact

            Width = CheckedPositiveDimension(ReadUInt32(0), "width")
            Height = CheckedPositiveDimension(ReadUInt32(4), "height")
            Dim channels As UInteger = ReadUInt32(8)
            Dim linearSize As UInteger = ReadUInt32(12)
            AlphaMean = BitConverter.ToSingle(Data, 16)

            Select Case channels
                Case 3UI
                    PixelStorage = DdsPixelStorage.Dxt1
                    PixelFlags = DDPF_FOURCC
                    FourCC = "DXT1"
                Case 4UI
                    PixelStorage = DdsPixelStorage.Dxt5
                    PixelFlags = DDPF_FOURCC Or DDPF_ALPHAPIXELS
                    FourCC = "DXT5"
                Case Else
                    Throw New InvalidDataException("Unsupported BioWare DDS channel count: " & channels.ToString())
            End Select

            If Not LooksLikeBioWarePayload(CInt(linearSize)) Then
                Throw New InvalidDataException("File is not a valid standard DDS or BioWare compact DDS.")
            End If

            BuildMipLevels(20, Integer.MaxValue, 0)
        End Sub

        Private Function LooksLikeBioWarePayload(LinearSize As Integer) As Boolean
            Dim firstMipSize As Integer = GetCompressedMipSize(Width, Height)
            Dim remaining As Integer = Data.Length - 20

            If remaining < firstMipSize Then Return False
            If LinearSize > 0 AndAlso LinearSize > remaining Then Return False

            Return True
        End Function

        Private Sub BuildMipLevels(DataOffset As Integer, DeclaredMipCount As Integer, PitchOrLinearSize As Integer)
            MipLevels.Clear()

            Dim offset As Integer = DataOffset
            Dim mipWidth As Integer = Width
            Dim mipHeight As Integer = Height
            Dim level As Integer = 0

            While offset < Data.Length AndAlso level < DeclaredMipCount
                Dim mipSize As Integer = GetMipDataSize(mipWidth, mipHeight, level, PitchOrLinearSize)
                If mipSize <= 0 Then Throw New InvalidDataException("DDS mipmap size is invalid.")

                If offset + mipSize > Data.Length Then
                    If level = 0 Then Throw New InvalidDataException("DDS pixel data is shorter than the first mipmap.")
                    Exit While
                End If

                MipLevels.Add(New DdsMipLevel With {.Width = mipWidth, .Height = mipHeight, .Offset = offset, .Size = mipSize})

                offset += mipSize
                If mipWidth = 1 AndAlso mipHeight = 1 Then Exit While

                mipWidth = Math.Max(1, mipWidth \ 2)
                mipHeight = Math.Max(1, mipHeight \ 2)
                level += 1
            End While

            If MipLevels.Count = 0 Then Throw New InvalidDataException("DDS file does not contain readable image data.")
        End Sub

        Private Function GetMipDataSize(MipWidth As Integer, MipHeight As Integer, MipLevel As Integer, PitchOrLinearSize As Integer) As Integer
            Select Case PixelStorage
                Case DdsPixelStorage.Dxt1, DdsPixelStorage.Dxt3, DdsPixelStorage.Dxt5
                    Return GetCompressedMipSize(MipWidth, MipHeight)
                Case DdsPixelStorage.Rgba
                    Dim rowPitch As Integer = GetRawRowPitch(MipWidth)
                    If MipLevel = 0 AndAlso (HeaderFlags And DDSD_PITCH) <> 0 AndAlso PitchOrLinearSize > rowPitch AndAlso PitchOrLinearSize <= Integer.MaxValue Then
                        rowPitch = CInt(PitchOrLinearSize)
                    End If
                    Return CheckedToInteger(CLng(rowPitch) * CLng(MipHeight), "DDS raw mipmap data is too large.")
            End Select

            Return 0
        End Function

        Private Function GetCompressedMipSize(MipWidth As Integer, MipHeight As Integer) As Integer
            Dim blocksWide As Integer = Math.Max(1, (MipWidth + 3) \ 4)
            Dim blocksHigh As Integer = Math.Max(1, (MipHeight + 3) \ 4)
            Return blocksWide * blocksHigh * GetCompressedBytesPerBlock()
        End Function

        Private Function GetCompressedBytesPerBlock() As Integer
            If PixelStorage = DdsPixelStorage.Dxt1 Then Return 8
            Return 16
        End Function

        Private Function GetRawRowPitch(MipWidth As Integer) As Integer
            Dim bytesPerPixel As Integer = Math.Max(1, (RgbBitCount + 7) \ 8)
            Return CheckedToInteger(CLng(MipWidth) * CLng(bytesPerPixel), "DDS raw row pitch is too large.")
        End Function

        Private Function CheckedToInteger(Value As Long, ErrorMessage As String) As Integer
            If Value <= 0 OrElse Value > Integer.MaxValue Then Throw New InvalidDataException(ErrorMessage)
            Return CInt(Value)
        End Function

        Private Function DecodeRawPixels(RawData As Byte(), MipWidth As Integer, MipHeight As Integer, MipLevel As Integer) As Byte()
            Dim bytesPerPixel As Integer = Math.Max(1, (RgbBitCount + 7) \ 8)
            Dim rowPitch As Integer = GetRawRowPitch(MipWidth)
            Dim mip As DdsMipLevel = MipLevels(MipLevel)
            rowPitch = Math.Max(rowPitch, mip.Size \ MipHeight)

            Dim rgba As Byte() = New Byte(MipWidth * MipHeight * 4 - 1) {}

            For y As Integer = 0 To MipHeight - 1
                Dim rowOffset As Integer = y * rowPitch

                For x As Integer = 0 To MipWidth - 1
                    Dim sourceOffset As Integer = rowOffset + x * bytesPerPixel
                    If sourceOffset + bytesPerPixel > RawData.Length Then Throw New InvalidDataException("DDS raw pixel data is truncated.")

                    Dim pixelValue As UInteger = 0UI
                    For i As Integer = 0 To bytesPerPixel - 1
                        pixelValue = pixelValue Or (CUInt(RawData(sourceOffset + i)) << (8 * i))
                    Next

                    Dim targetOffset As Integer = (y * MipWidth + x) * 4
                    rgba(targetOffset) = ExtractMaskValue(pixelValue, RBitMask)
                    rgba(targetOffset + 1) = ExtractMaskValue(pixelValue, GBitMask)
                    rgba(targetOffset + 2) = ExtractMaskValue(pixelValue, BBitMask)
                    rgba(targetOffset + 3) = If(ABitMask = 0UI, CByte(255), ExtractMaskValue(pixelValue, ABitMask))
                Next
            Next

            Return rgba
        End Function

        Private Function ExtractMaskValue(PixelValue As UInteger, Mask As UInteger) As Byte
            If Mask = 0UI Then Return 0

            Dim shift As Integer = 0
            Dim shiftedMask As UInteger = Mask
            While (shiftedMask And 1UI) = 0UI
                shiftedMask >>= 1
                shift += 1
            End While

            Dim maxValue As UInteger = shiftedMask
            Dim value As UInteger = (PixelValue And Mask) >> shift
            If maxValue = 0UI Then Return 0

            Return CByte((value * 255UI + (maxValue \ 2UI)) \ maxValue)
        End Function

        Private Function GetDx10PixelStorage(Format As UInteger) As DdsPixelStorage
            Select Case Format
                Case 70UI, 71UI, 72UI
                    Return DdsPixelStorage.Dxt1
                Case 73UI, 74UI, 75UI
                    Return DdsPixelStorage.Dxt3
                Case 76UI, 77UI, 78UI
                    Return DdsPixelStorage.Dxt5
                Case 28UI, 29UI
                    RgbBitCount = 32
                    RBitMask = &HFFUI
                    GBitMask = &HFF00UI
                    BBitMask = &HFF0000UI
                    ABitMask = &HFF000000UI
                    Return DdsPixelStorage.Rgba
                Case 87UI, 91UI
                    RgbBitCount = 32
                    BBitMask = &HFFUI
                    GBitMask = &HFF00UI
                    RBitMask = &HFF0000UI
                    ABitMask = &HFF000000UI
                    Return DdsPixelStorage.Rgba
                Case 88UI, 93UI
                    RgbBitCount = 32
                    BBitMask = &HFFUI
                    GBitMask = &HFF00UI
                    RBitMask = &HFF0000UI
                    ABitMask = 0UI
                    Return DdsPixelStorage.Rgba
            End Select

            Throw New InvalidDataException("Unsupported DDS DXGI format: " & Format.ToString())
        End Function

        Private Function GetDxtFlags() As Integer
            Select Case PixelStorage
                Case DdsPixelStorage.Dxt1
                    Dim hasDxt1Alpha As Boolean = (PixelFlags And DDPF_ALPHAPIXELS) <> 0
                    Return CInt(DXTFlags.DXT1) Or If(hasDxt1Alpha, CInt(DXTFlags.DXT1Alpha), 0)
                Case DdsPixelStorage.Dxt3
                    Return CInt(DXTFlags.DXT3)
                Case DdsPixelStorage.Dxt5
                    Return CInt(DXTFlags.DXT5)
            End Select

            Return 0
        End Function

        Private Function ClampMipmapLevel(MipmapLevel As Integer) As Integer
            If MipLevels.Count = 0 Then Return 0
            If MipmapLevel < 0 Then Return 0
            If MipmapLevel >= MipLevels.Count Then Return MipLevels.Count - 1
            Return MipmapLevel
        End Function

        Private Function CheckedPositiveDimension(Value As UInteger, Name As String) As Integer
            If Value = 0UI OrElse Value > Integer.MaxValue Then Throw New InvalidDataException("DDS " & Name & " is invalid.")
            Return CInt(Value)
        End Function

        Private Function ReadUInt32(Offset As Integer) As UInteger
            If Offset < 0 OrElse Offset + 4 > Data.Length Then Throw New EndOfStreamException("Unexpected end of DDS header.")
            Return BitConverter.ToUInt32(Data, Offset)
        End Function

        Private Function ReadBytes(Offset As Integer, Count As Integer) As Byte()
            If Offset < 0 OrElse Count < 0 OrElse Offset + Count > Data.Length Then Throw New EndOfStreamException("Unexpected end of DDS pixel data.")

            Dim bytes As Byte() = New Byte(Count - 1) {}
            Buffer.BlockCopy(Data, Offset, bytes, 0, Count)
            Return bytes
        End Function

        Private Sub ConvertRgbaToBgra(ByRef Pixels As Byte())
            For i As Integer = 0 To Pixels.Length - 1 Step 4
                Dim tmp As Byte = Pixels(i)
                Pixels(i) = Pixels(i + 2)
                Pixels(i + 2) = tmp
            Next
        End Sub

        Private Function CountMaskBits(Mask As UInteger) As Integer
            Dim count As Integer = 0
            While Mask <> 0UI
                If (Mask And 1UI) <> 0UI Then count += 1
                Mask >>= 1
            End While
            Return count
        End Function

    End Class

End Module
