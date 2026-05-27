Imports System.Runtime.InteropServices

Module DXTDecompression

    Public Enum DXTFlags As Integer
        DXT1 = 1 << 0
        DXT3 = 1 << 1
        DXT5 = 1 << 2
        DXT1Alpha = 1 << 3
    End Enum

    Private Sub Decompress(ByRef rgba As Byte(), ByVal block As Byte(), ByVal flags As Integer)
        ' get the block locations
        Dim colourBlock As Byte() = New Byte(7) {}
        Dim alphaBlock As Byte() = block

        If (flags And (DXTFlags.DXT3 Or CInt(DXTFlags.DXT5))) <> 0 Then
            Array.Copy(block, 8, colourBlock, 0, 8)
        Else
            Array.Copy(block, 0, colourBlock, 0, 8)
        End If
        ' decompress color
        DecompressColor(rgba, colourBlock, (flags And DXTFlags.DXT1) <> 0, (flags And DXTFlags.DXT1Alpha) <> 0)
        ' decompress alpha separately if necessary
        If (flags And DXTFlags.DXT3) <> 0 Then
            DecompressAlphaDxt3(rgba, alphaBlock)
        ElseIf (flags And DXTFlags.DXT5) <> 0 Then
            DecompressAlphaDxt5(rgba, alphaBlock)
        End If
    End Sub

    Private Sub DecompressAlphaDxt3(ByRef rgba As Byte(), ByVal block As Byte())
        Dim bytes As Byte() = block


        ' Unpack the alpha values pairwise
        For i As Integer = 0 To 8 - 1
            ' Quantise down to 4 bits
            Dim quant As Byte = bytes(i)
            Dim lo As Byte = quant And &HF
            Dim hi As Byte = quant And &HF0

            ' Convert back up to bytes
            rgba(8 * i + 3) = lo Or lo << 4
            rgba(8 * i + 7) = hi Or hi >> 4
        Next
    End Sub

    Private Sub DecompressAlphaDxt5(ByRef rgba As Byte(), ByVal block As Byte())
        ' Get the two alpha values
        Dim bytes As Byte() = block
        Dim alpha0 As Integer = bytes(0)
        Dim alpha1 As Integer = bytes(1)

        ' compare the values to build the codebook
        Dim codes As Byte() = New Byte(7) {}
        codes(0) = CByte(alpha0)
        codes(1) = CByte(alpha1)

        If alpha0 <= alpha1 Then

            ' Use 5-Alpha Codebook
            For i As Integer = 1 To 5 - 1
                codes(1 + i) = CByte(((5 - i) * alpha0 + i * alpha1) \ 5)
            Next

            codes(6) = 0
            codes(7) = 255
        Else

            ' Use 7-Alpha Codebook
            For i As Integer = 1 To 7 - 1
                codes(i + 1) = CByte(((7 - i) * alpha0 + i * alpha1) \ 7)
            Next
        End If


        ' decode indices
        Dim indices As Byte() = New Byte(15) {}
        Dim blockSrc_pos As Integer = 2
        Dim indices_pos As Integer = 0

        For i As Integer = 0 To 2 - 1
            ' grab 3 bytes
            Dim value As Integer = 0

            For j As Integer = 0 To 3 - 1
                Dim _byte As Integer = bytes(blockSrc_pos)
                blockSrc_pos += 1
                value = value Or _byte << 8 * j
            Next


            ' unpack 8 3-bit values from it
            For j As Integer = 0 To 8 - 1
                Dim index As Integer = value >> 3 * j And &H7
                indices(indices_pos) = CByte(index)
                indices_pos += 1
            Next
        Next


        ' write out the indexed coebook values
        For i As Integer = 0 To 16 - 1
            rgba(4 * i + 3) = codes(indices(i))
        Next
    End Sub

    Private Sub DecompressColor(ByRef rgba As Byte(), ByVal block As Byte(), ByVal isDxt1 As Boolean, ByVal dxt1HasAlpha As Boolean)
        Dim bytes As Byte() = block

        ' Unpack Endpoints
        Dim codes As Byte() = New Byte(15) {}
        Dim a As Integer = Unpack(bytes, 0, codes, 0)
        Dim b As Integer = Unpack(bytes, 2, codes, 4)


        ' generate Midpoints
        For i As Integer = 0 To 3 - 1
            Dim c As Integer = codes(i)
            Dim d As Integer = codes(4 + i)

            If isDxt1 AndAlso a <= b Then
                codes(8 + i) = CByte((c + d) \ 2)
                codes(12 + i) = 0
            Else
                codes(8 + i) = CByte((2 * c + d) \ 3)
                codes(12 + i) = CByte((c + 2 * d) \ 3)
            End If
        Next


        ' Fill in alpha for intermediate values
        codes(8 + 3) = 255
        codes(12 + 3) = If(isDxt1 AndAlso a <= b AndAlso dxt1HasAlpha, CByte(0), CByte(255))

        'unpack the indices
        Dim indices As Byte() = New Byte(15) {}

        For i As Integer = 0 To 4 - 1
            Dim packed As Byte = bytes(4 + i)
            indices(0 + i * 4) = CByte(packed And &H3)
            indices(1 + i * 4) = CByte(packed >> 2 And &H3)
            indices(2 + i * 4) = CByte(packed >> 4 And &H3)
            indices(3 + i * 4) = CByte(packed >> 6 And &H3)
        Next


        ' store out the colours
        For i As Integer = 0 To 16 - 1
            Dim offset As Byte = 4 * indices(i)

            For j As Integer = 0 To 4 - 1
                rgba(4 * i + j) = codes(offset + j)
            Next
        Next
    End Sub

    ''previos name was unpack565 no idea what the 565 means so I removed it 
    Private Function Unpack(ByVal packed As Byte(), ByVal packed_offset As Integer, ByRef colour As Byte(), ByVal colour_offset As Integer) As Integer
        ' Build packed value 
        Dim value As Integer = packed(0 + packed_offset) Or CInt(packed(1 + packed_offset)) << 8

        ' get components in the stored range
        Dim red As Byte = value >> 11 And &H1F
        Dim green As Byte = value >> 5 And &H3F
        Dim blue As Byte = value And &H1F

        ' Scale up to 8 Bit
        colour(0 + colour_offset) = red << 3 Or red >> 2
        colour(1 + colour_offset) = green << 2 Or green >> 4
        colour(2 + colour_offset) = blue << 3 Or blue >> 2
        colour(3 + colour_offset) = 255
        Return value
    End Function

    Public Sub DecompressImage(<Out> ByRef rgba As Byte(), ByVal width As Integer, ByVal height As Integer, ByVal blocks As Byte(), ByVal flags As Integer)
        'rgba = New Byte(width * height * 4 - 1) {}

        ' initialise the block input
        Dim sourceBlock As Byte() = blocks
        Dim sourceBlock_pos As Integer = 0
        Dim bytesPerBlock As Integer = If((flags And DXTFlags.DXT1) <> 0, 8, 16)


        ' loop over blocks
        For y As Integer = 0 To height - 1 Step 4

            For x As Integer = 0 To width - 1 Step 4
                ' decompress the block
                Dim targetRGBA As Byte() = New Byte(63) {}
                Dim targetRGBA_pos As Integer = 0
                Dim sourceBlockBuffer As Byte() = New Byte(bytesPerBlock - 1) {} ' größe korrekt?
                If sourceBlock_pos >= sourceBlock.Length Then Exit For
                Array.Copy(sourceBlock, sourceBlock_pos, sourceBlockBuffer, 0, Math.Min(bytesPerBlock, sourceBlock.Length - sourceBlock_pos))
                'sourceBlock.CopyTo(sourceBlockBuffer, sourceBlock_pos);
                Decompress(targetRGBA, sourceBlockBuffer, flags)

                ' Write the decompressed pixels to the correct image locations
                Dim sourcePixel As Byte() = New Byte(3) {}

                For py As Integer = 0 To 4 - 1

                    For px As Integer = 0 To 4 - 1
                        Dim sx As Integer = x + px
                        Dim sy As Integer = y + py

                        If sx < width AndAlso sy < height Then
                            Dim targetPixel As Integer = 4 * (width * sy + sx)

                            'targetRGBA.CopyTo(sourcePixel, targetRGBA_pos);
                            Array.Copy(targetRGBA, targetRGBA_pos, sourcePixel, 0, 4)
                            targetRGBA_pos += 4

                            For i As Integer = 0 To 4 - 1
                                rgba(targetPixel + i) = sourcePixel(i)
                            Next
                        Else
                            ' Ignore that pixel
                            targetRGBA_pos += 4
                        End If
                    Next
                Next

                sourceBlock_pos += bytesPerBlock
            Next
        Next
    End Sub




End Module
