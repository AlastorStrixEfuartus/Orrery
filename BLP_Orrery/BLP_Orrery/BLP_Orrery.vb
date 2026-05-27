Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.InteropServices

Public Class BLP_Orrery_MainForm
    Implements IMessageFilter

    Private Const ApplicationTitle As String = "BLP Orrery"
    Private Const CurrentVersion As String = "1.9"
    Private Const AuthorName As String = "Alastor Strix'Efuartus"
    Private Const DevelopmentStartYear As String = "2022"
    Private Const RegistryPath As String = "HKEY_CURRENT_USER\WOWBLP_Orrery"
    Private Const SplashArtRelativePath As String = "Assets\OrrerySplashArt.png"
    Private Const MinPreviewZoomFactor As Single = 0.0625F
    Private Const MaxPreviewZoomFactor As Single = 32.0F
    Private Const PreviewZoomStep As Single = 1.25F
    Private Const NavigationCoalesceIntervalMs As Integer = 55
    Private Const WM_MOUSEWHEEL As Integer = &H20A

    Private CurrentFilePath As String = String.Empty
    Private CurrentMipMapIndex As Integer = 0
    Private CurrentSourceBitmap As Bitmap = Nothing
    Private MaskModeEnabled As Boolean = False
    Private IsLoadingImage As Boolean = False
    Private MainContextMenu As ContextMenuStrip = Nothing
    Private TransparencyBackgroundColor As Color = Color.Black
    Private PreviewZoomFactor As Single = 1.0F
    Private PreviewZoomIsManual As Boolean = False
    Private SiblingImageCacheDirectory As String = String.Empty
    Private SiblingImageCacheFiles As String() = New String() {}
    Private SiblingImageCacheLastWriteUtc As DateTime = DateTime.MinValue
    Private NavigationTimer As Timer = Nothing
    Private PendingNavigationFilePath As String = String.Empty
    Private PendingNavigationIndex As Integer = -1
    Private ReadOnly SupportedImageExtensions As String() = {".BLP", ".DDS", ".TGA", ".ICO", ".PNG", ".JPG", ".JPEG"}

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Left Then
            QueueSiblingImageNavigation(-1)
            Return True
        End If

        If keyData = Keys.Right Then
            QueueSiblingImageNavigation(1)
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        Application.RemoveMessageFilter(Me)
        SaveApplicationSettings()
        DisposeNavigationTimer()
        DisposeCurrentImages()
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Application.AddMessageFilter(Me)
        LoadApplicationSettings()
        UpdateWindowTitle()
        EnableMainContextMenu()
        EnableMainFrameDragDrop()
        PositionNavigationButtons()
        UpdateDisplayButtons()
        UpdateNavigationButtons()
        OpenStartupImageFromCommandLine(Environment.GetCommandLineArgs())
    End Sub

    Private Sub LoadApplicationSettings()
        FileInformationsToolStripMenuItem.CheckOnClick = True
        ResizeByTextureMenuItem.CheckOnClick = True

        Dim savedIndex As Object = My.Computer.Registry.GetValue(RegistryPath, "SaveAsFormatIndex", Nothing)
        Dim selectedIndex As Integer = 0

        If savedIndex IsNot Nothing Then
            Integer.TryParse(savedIndex.ToString(), selectedIndex)
        End If

        If selectedIndex < 0 OrElse selectedIndex >= ComBxSaveAsFormat.Items.Count Then selectedIndex = 0
        ComBxSaveAsFormat.SelectedIndex = selectedIndex

        Dim savedRenderTransparency As Object = My.Computer.Registry.GetValue(RegistryPath, "RenderTransparency", Nothing)
        Dim renderTransparency As Boolean = False
        If savedRenderTransparency IsNot Nothing Then
            Boolean.TryParse(savedRenderTransparency.ToString(), renderTransparency)
        End If
        RenderTransparencyMenuItem.Checked = renderTransparency

        Dim savedColorArgb As Object = My.Computer.Registry.GetValue(RegistryPath, "TransparencyBackgroundArgb", Nothing)
        Dim colorArgb As Integer = Color.Black.ToArgb()
        If savedColorArgb IsNot Nothing Then
            Integer.TryParse(savedColorArgb.ToString(), colorArgb)
        End If

        SetTransparencyBackgroundColor(Color.FromArgb(colorArgb), False)

        Dim savedFileInformationVisible As Object = My.Computer.Registry.GetValue(RegistryPath, "FileInformationVisible", Nothing)
        Dim fileInformationVisible As Boolean = True
        If savedFileInformationVisible IsNot Nothing Then
            Boolean.TryParse(savedFileInformationVisible.ToString(), fileInformationVisible)
        End If

        FileInformationsToolStripMenuItem.Checked = fileInformationVisible
        PnlFileInfo.Visible = fileInformationVisible

        Dim savedResizeByTexture As Object = My.Computer.Registry.GetValue(RegistryPath, "ResizeByTexture", Nothing)
        Dim resizeByTexture As Boolean = False
        If savedResizeByTexture IsNot Nothing Then
            Boolean.TryParse(savedResizeByTexture.ToString(), resizeByTexture)
        End If

        ResizeByTextureMenuItem.Checked = resizeByTexture
        UpdateTextureViewSizing()
    End Sub

    Private Sub SaveApplicationSettings()
        My.Computer.Registry.SetValue(RegistryPath, "SaveAsFormatIndex", ComBxSaveAsFormat.SelectedIndex)
        My.Computer.Registry.SetValue(RegistryPath, "RenderTransparency", RenderTransparencyMenuItem.Checked.ToString())
        My.Computer.Registry.SetValue(RegistryPath, "TransparencyBackgroundArgb", TransparencyBackgroundColor.ToArgb().ToString())
        My.Computer.Registry.SetValue(RegistryPath, "FileInformationVisible", FileInformationsToolStripMenuItem.Checked.ToString())
        My.Computer.Registry.SetValue(RegistryPath, "ResizeByTexture", ResizeByTextureMenuItem.Checked.ToString())
    End Sub

    Private Sub OpenStartupImageFromCommandLine(args As String())
        If args Is Nothing OrElse args.Length <= 1 Then Return

        For i As Integer = 1 To args.Length - 1
            Dim filePath As String = NormalizeCommandLineFilePath(args(i))
            If File.Exists(filePath) AndAlso IsSupportedImageFile(filePath) Then
                OpenImageFile(filePath)
                Return
            End If
        Next
    End Sub

    Private Function NormalizeCommandLineFilePath(FilePath As String) As String
        If String.IsNullOrWhiteSpace(FilePath) Then Return String.Empty

        FilePath = FilePath.Trim().Trim(""""c)

        If FilePath.StartsWith("file:", StringComparison.OrdinalIgnoreCase) Then
            Dim fileUri As Uri = Nothing
            If Uri.TryCreate(FilePath, UriKind.Absolute, fileUri) AndAlso fileUri.IsFile Then
                Return fileUri.LocalPath
            End If
        End If

        Return FilePath
    End Function

    Private Sub ImageSave(sender As Object, e As EventArgs) Handles BtnSave.Click, SaveAsToolStripMenuItem.Click
        If PicBxTextureView.Image Is Nothing OrElse String.IsNullOrWhiteSpace(TxBxTextureName.Text) Then
            MessageBox.Show("Open an image before saving.", ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim selectedFormat As String = "PNG"
        If ComBxSaveAsFormat.SelectedItem IsNot Nothing Then selectedFormat = ComBxSaveAsFormat.SelectedItem.ToString()
        If Not selectedFormat.Equals("JPG", StringComparison.OrdinalIgnoreCase) Then selectedFormat = "PNG"

        Dim extension As String = If(selectedFormat.Equals("JPG", StringComparison.OrdinalIgnoreCase), "jpg", "png")
        Dim textureName As String = Path.GetFileNameWithoutExtension(TxBxTextureName.Text)

        Using saveDialog As New SaveFileDialog()
            saveDialog.Title = "Save Image As"
            saveDialog.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg"
            saveDialog.FilterIndex = If(extension = "jpg", 2, 1)
            saveDialog.AddExtension = True
            saveDialog.OverwritePrompt = True
            saveDialog.FileName = textureName & "." & extension

            If Directory.Exists(TxBxTextureDirectory.Text) Then
                saveDialog.InitialDirectory = TxBxTextureDirectory.Text
            ElseIf Not String.IsNullOrWhiteSpace(CurrentFilePath) AndAlso File.Exists(CurrentFilePath) Then
                saveDialog.InitialDirectory = Path.GetDirectoryName(CurrentFilePath)
            End If

            If saveDialog.ShowDialog(Me) <> DialogResult.OK Then Return

            If Path.GetExtension(saveDialog.FileName).Equals(".jpg", StringComparison.OrdinalIgnoreCase) OrElse
               Path.GetExtension(saveDialog.FileName).Equals(".jpeg", StringComparison.OrdinalIgnoreCase) Then
                SaveCurrentPreviewAsJpeg(saveDialog.FileName)
            Else
                SaveCurrentPreviewAsPng(saveDialog.FileName)
            End If
        End Using
    End Sub

    Private Sub SaveCurrentPreviewAsPng(OutputPath As String)
        Using saveCopy As New Bitmap(PicBxTextureView.Image)
            saveCopy.Save(OutputPath, ImageFormat.Png)
        End Using
    End Sub

    Private Sub SaveCurrentPreviewAsJpeg(OutputPath As String)
        Using saveCopy As New Bitmap(PicBxTextureView.Image.Width, PicBxTextureView.Image.Height, PixelFormat.Format24bppRgb)
            Using graphics As Graphics = Graphics.FromImage(saveCopy)
                graphics.Clear(TransparencyBackgroundColor)
                Using imageAttributes As New ImageAttributes()
                    Dim matrix As New ColorMatrix()
                    matrix.Matrix33 = 1.0F
                    imageAttributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap)
                    graphics.DrawImage(PicBxTextureView.Image,
                                       New Rectangle(0, 0, saveCopy.Width, saveCopy.Height),
                                       0,
                                       0,
                                       PicBxTextureView.Image.Width,
                                       PicBxTextureView.Image.Height,
                                       GraphicsUnit.Pixel,
                                       imageAttributes)
                End Using
            End Using
            saveCopy.Save(OutputPath, ImageFormat.Jpeg)
        End Using
    End Sub

    Private Sub TxtbxWoWPath_TextChanged(sender As Object, e As EventArgs) Handles TxBxTextureDirectory.TextChanged
        If TxBxTextureDirectory.Text.Length > 1 AndAlso Not TxBxTextureDirectory.Text.EndsWith("\") Then
            TxBxTextureDirectory.Text = EnsureTrailingSlash(TxBxTextureDirectory.Text)
            TxBxTextureDirectory.SelectionStart = TxBxTextureDirectory.Text.Length
        End If
    End Sub

    Private Sub OpenImage_OFD1_Click(sender As Object, e As EventArgs) 
        OFD1TextureToSplit.Filter = "All Supported Image Files|*.BLP;*.DDS;*.TGA;*.ICO;*.PNG;*.JPG;*.JPEG|" &
                                    "Blizzard Picture (*.BLP)|*.BLP|" &
                                    "DirectDraw Surface (*.DDS)|*.DDS|" &
                                    "Truevision TGA (*.TGA)|*.TGA|" &
                                    "Windows Icon (*.ICO)|*.ICO|" &
                                    "Portable Network Graphic (*.PNG)|*.PNG|" &
                                    "JPEG Image (*.JPG;*.JPEG)|*.JPG;*.JPEG"

        If Directory.Exists(TxBxTextureDirectory.Text) Then
            OFD1TextureToSplit.InitialDirectory = TxBxTextureDirectory.Text
        ElseIf Not String.IsNullOrWhiteSpace(CurrentFilePath) AndAlso File.Exists(CurrentFilePath) Then
            OFD1TextureToSplit.InitialDirectory = Path.GetDirectoryName(CurrentFilePath)
        End If

        If OFD1TextureToSplit.ShowDialog = DialogResult.OK Then
            OpenImageFile(OFD1TextureToSplit.FileName)
        End If
    End Sub

    Private Sub OpenImageFile(FilePath As String, Optional FromQueuedNavigation As Boolean = False)
        If String.IsNullOrWhiteSpace(FilePath) Then Return
        If Not FromQueuedNavigation Then CancelQueuedSiblingNavigation()

        FilePath = Path.GetFullPath(FilePath)
        If Not File.Exists(FilePath) Then
            MessageBox.Show("File not found:" & Environment.NewLine & FilePath, "BLP Orrery", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If Not IsSupportedImageFile(FilePath) Then
            MessageBox.Show("Unsupported image type:" & Environment.NewLine & Path.GetExtension(FilePath), "BLP Orrery", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        LoadImage(FilePath)
    End Sub

    Private Sub LoadImage(FilePath As String)
        Try
            IsLoadingImage = True
            CurrentFilePath = Path.GetFullPath(FilePath)
            CurrentMipMapIndex = 0

            TxBxTextureName.Text = Path.GetFileName(CurrentFilePath)
            TxBxTextureDirectory.Text = EnsureTrailingSlash(Path.GetDirectoryName(CurrentFilePath))
            ResetFileInfo()
            ResetPreviewZoomForNewImage()

            If IsBlpFile(CurrentFilePath) Then
                LoadBlpImage(CurrentFilePath)
            ElseIf IsDdsFile(CurrentFilePath) Then
                LoadDdsImage(CurrentFilePath)
            ElseIf IsTgaFile(CurrentFilePath) Then
                LoadTgaImage(CurrentFilePath)
            ElseIf IsIcoFile(CurrentFilePath) Then
                LoadIcoImage(CurrentFilePath)
            Else
                LoadStandardImage(CurrentFilePath)
            End If

            UpdateWindowTitle(CurrentFilePath)

        Catch ex As Exception
            MessageBox.Show("Could not load image:" & Environment.NewLine & ex.Message, "BLP Orrery", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            IsLoadingImage = False
            PositionNavigationButtons()
            UpdateDisplayButtons()
            UpdateNavigationButtons()
        End Try
    End Sub

    Private Sub LoadBlpImage(FilePath As String)
        Using fileStream As New FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Using BLP = New BlpFile(fileStream)
                If BLP.GetIsValidVersion = False Then Return
                If BLP.MipMapCount <= 0 Then Throw New InvalidDataException("This BLP file does not contain readable mipmap data.")

                PopulateBlpInfo(BLP)
                CurrentMipMapIndex = 0
                SetSourceBitmap(BLP.GetBitmap(CurrentMipMapIndex))
                SetActiveMipmapInfo(CurrentMipMapIndex, BLP.GetMipmapWidth(CurrentMipMapIndex), BLP.GetMipmapHeight(CurrentMipMapIndex))

                If LsBxMipMapList.Items.Count > 1 Then LsBxMipMapList.SelectedIndex = 1
            End Using
        End Using
    End Sub

    Private Sub LoadDdsImage(FilePath As String)
        Using fileStream As New FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Dim DDS As New DdsFile(fileStream)
            If DDS.MipMapCount <= 0 Then Throw New InvalidDataException("This DDS file does not contain readable mipmap data.")

            PopulateDdsInfo(DDS)
            CurrentMipMapIndex = 0
            SetSourceBitmap(DDS.GetBitmap(CurrentMipMapIndex))
            SetActiveMipmapInfo(CurrentMipMapIndex, DDS.GetMipmapWidth(CurrentMipMapIndex), DDS.GetMipmapHeight(CurrentMipMapIndex))

            If LsBxMipMapList.Items.Count > 1 Then LsBxMipMapList.SelectedIndex = 1
        End Using
    End Sub

    Private Sub LoadTgaImage(FilePath As String)
        Using fileStream As New FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Dim TGA As New TgaFile(fileStream)
            PopulateTgaInfo(TGA)
            CurrentMipMapIndex = 0
            SetSourceBitmap(TGA.GetBitmap())
            SetActiveMipmapInfo(0, TGA.GetTgaWidth(), TGA.GetTgaHeight())

            If LsBxMipMapList.Items.Count > 1 Then LsBxMipMapList.SelectedIndex = 1
        End Using
    End Sub

    Private Sub LoadIcoImage(FilePath As String)
        Using fileStream As New FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Dim ICO As New IcoFile(fileStream)
            If ICO.ImageCount <= 0 Then Throw New InvalidDataException("This ICO file does not contain readable image data.")

            PopulateIcoInfo(ICO)
            CurrentMipMapIndex = ICO.GetBestImageIndex()
            SetSourceBitmap(ICO.GetBitmap(CurrentMipMapIndex))
            SetActiveIcoInfo(ICO, CurrentMipMapIndex)

            If LsBxMipMapList.Items.Count > 1 Then LsBxMipMapList.SelectedIndex = CurrentMipMapIndex + 1
        End Using
    End Sub

    Private Sub LoadStandardImage(FilePath As String)
        Using loadedImage As Image = Image.FromFile(FilePath)
            SetSourceBitmap(CloneAs32BppArgb(loadedImage))
            LblResolutionValue.Text = loadedImage.Width.ToString() & " x " & loadedImage.Height.ToString()
            UpdateTextureViewSizing(loadedImage.Width, loadedImage.Height)
        End Using

        LblCompressionValue.Text = Path.GetExtension(FilePath).TrimStart("."c).ToUpperInvariant()
        LblAlphaEncodingValue.Text = "N/A"
        LblAlphaChannelValue.Text = "N/A"
        LblMipMapCountValue.Text = "0"
        LblActiveMipMaipValue.Text = "N/A"
    End Sub

    Private Sub PopulateBlpInfo(BLP As BlpFile)
        LsBxMipMapList.Items.Clear()
        LsBxMipMapList.Items.Add(" # | Size      | Bytes    | Offset")

        For i As Integer = 0 To BLP.MipMapCount - 1
            LsBxMipMapList.Items.Add(String.Format("{0,2} | {1,4}x{2,-4} | {3,8} | {4}",
                                                    i,
                                                    BLP.GetMipmapWidth(i),
                                                    BLP.GetMipmapHeight(i),
                                                    BLP.GetBLPMipMapSize(i),
                                                    BLP.GetBLPMipMapOffset(i)))
        Next

        LblMipMapCountValue.Text = BLP.MipMapCount.ToString()

        If BLP.GetBLPEncoding = 1 Then
            LblCompressionValue.Text = "Uncompressed"
            If BLP.GetBLPAlphaDepth = 8 Then
                LblAlphaEncodingValue.Text = "Palette + Alpha"
            ElseIf BLP.GetBLPAlphaDepth > 0 Then
                LblAlphaEncodingValue.Text = "Palette"
            Else
                LblAlphaEncodingValue.Text = "Palette Only"
            End If
        ElseIf BLP.GetBLPEncoding = 2 Then
            LblCompressionValue.Text = "DXTC"
            If BLP.GetBLPAlphaEncoding = 0 Then
                LblAlphaEncodingValue.Text = "DXT1"
            ElseIf BLP.GetBLPAlphaEncoding = 1 Then
                LblAlphaEncodingValue.Text = "DXT3"
            ElseIf BLP.GetBLPAlphaEncoding = 7 Then
                LblAlphaEncodingValue.Text = "DXT5"
            Else
                LblAlphaEncodingValue.Text = "DXT"
            End If
        ElseIf BLP.GetBLPEncoding = 3 Then
            LblCompressionValue.Text = "Uncompressed"
            LblAlphaEncodingValue.Text = "RAW3 BGRA"
        End If

        If BLP.GetBLPAlphaDepth = 1 Then
            LblAlphaChannelValue.Text = "1 bit"
        Else
            LblAlphaChannelValue.Text = BLP.GetBLPAlphaDepth.ToString() & " bits"
        End If
    End Sub

    Private Sub PopulateDdsInfo(DDS As DdsFile)
        LsBxMipMapList.Items.Clear()
        LsBxMipMapList.Items.Add(" # | Size      | Bytes    | Offset")

        For i As Integer = 0 To DDS.MipMapCount - 1
            LsBxMipMapList.Items.Add(String.Format("{0,2} | {1,4}x{2,-4} | {3,8} | {4}",
                                                    i,
                                                    DDS.GetMipmapWidth(i),
                                                    DDS.GetMipmapHeight(i),
                                                    DDS.GetMipMapSize(i),
                                                    DDS.GetMipMapOffset(i)))
        Next

        LblCompressionValue.Text = DDS.GetContainerName()
        LblAlphaEncodingValue.Text = DDS.GetFormatName()
        LblAlphaChannelValue.Text = DDS.GetAlphaChannelText()
        LblMipMapCountValue.Text = DDS.MipMapCount.ToString()
    End Sub

    Private Sub PopulateTgaInfo(TGA As TgaFile)
        LsBxMipMapList.Items.Clear()
        LsBxMipMapList.Items.Add(" # | Size      | Bytes    | Offset")
        LsBxMipMapList.Items.Add(String.Format("{0,2} | {1,4}x{2,-4} | {3,8} | {4}",
                                                0,
                                                TGA.GetTgaWidth(),
                                                TGA.GetTgaHeight(),
                                                CLng(TGA.GetTgaWidth()) * CLng(TGA.GetTgaHeight()) * CLng(Math.Max(1, (TGA.GetPixelDepth() + 7) \ 8)),
                                                "N/A"))

        LblCompressionValue.Text = "TGA"
        LblAlphaEncodingValue.Text = TGA.GetFormatName() & " " & TGA.GetPixelDepth().ToString() & "-bit"
        LblAlphaChannelValue.Text = TGA.GetAlphaChannelText()
        LblMipMapCountValue.Text = "1"
    End Sub

    Private Sub PopulateIcoInfo(ICO As IcoFile)
        LsBxMipMapList.Items.Clear()
        LsBxMipMapList.Items.Add(" # | Size      | Bytes    | Offset")

        For i As Integer = 0 To ICO.ImageCount - 1
            LsBxMipMapList.Items.Add(String.Format("{0,2} | {1,4}x{2,-4} | {3,8} | {4}",
                                                    i,
                                                    ICO.GetIconWidth(i),
                                                    ICO.GetIconHeight(i),
                                                    ICO.GetIconImageSize(i),
                                                    ICO.GetIconImageOffset(i)))
        Next

        LblCompressionValue.Text = "ICO"
        LblAlphaEncodingValue.Text = "Icon directory"
        LblAlphaChannelValue.Text = "Mixed"
        LblMipMapCountValue.Text = ICO.ImageCount.ToString()
    End Sub

    Private Sub SetActiveIcoInfo(ICO As IcoFile, ImageIndex As Integer)
        Dim bitDepth As Integer = ICO.GetBitsPerPixel(ImageIndex)
        Dim formatText As String = ICO.GetFormatName(ImageIndex)
        If bitDepth > 0 Then formatText &= " " & bitDepth.ToString() & "-bit"

        LblAlphaEncodingValue.Text = formatText
        LblAlphaChannelValue.Text = ICO.GetAlphaChannelText(ImageIndex)
        SetActiveMipmapInfo(ImageIndex, ICO.GetIconWidth(ImageIndex), ICO.GetIconHeight(ImageIndex))
    End Sub

    Private Sub LoadCurrentBlpMipmap(MipmapIndex As Integer)
        If String.IsNullOrWhiteSpace(CurrentFilePath) OrElse Not IsBlpFile(CurrentFilePath) Then Return

        Try
            Using fileStream As New FileStream(CurrentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Using BLP = New BlpFile(fileStream)
                    If BLP.GetIsValidVersion = False Then Return
                    If BLP.MipMapCount <= 0 Then Return

                    If MipmapIndex < 0 Then MipmapIndex = 0
                    If MipmapIndex >= BLP.MipMapCount Then MipmapIndex = BLP.MipMapCount - 1

                    CurrentMipMapIndex = MipmapIndex
                    SetSourceBitmap(BLP.GetBitmap(CurrentMipMapIndex))
                    SetActiveMipmapInfo(CurrentMipMapIndex, BLP.GetMipmapWidth(CurrentMipMapIndex), BLP.GetMipmapHeight(CurrentMipMapIndex))
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Could not load mipmap " & MipmapIndex.ToString() & ":" & Environment.NewLine & ex.Message, "BLP Orrery", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadCurrentDdsMipmap(MipmapIndex As Integer)
        If String.IsNullOrWhiteSpace(CurrentFilePath) OrElse Not IsDdsFile(CurrentFilePath) Then Return

        Try
            Using fileStream As New FileStream(CurrentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Dim DDS As New DdsFile(fileStream)
                If DDS.MipMapCount <= 0 Then Return

                If MipmapIndex < 0 Then MipmapIndex = 0
                If MipmapIndex >= DDS.MipMapCount Then MipmapIndex = DDS.MipMapCount - 1

                CurrentMipMapIndex = MipmapIndex
                SetSourceBitmap(DDS.GetBitmap(CurrentMipMapIndex))
                SetActiveMipmapInfo(CurrentMipMapIndex, DDS.GetMipmapWidth(CurrentMipMapIndex), DDS.GetMipmapHeight(CurrentMipMapIndex))
            End Using
        Catch ex As Exception
            MessageBox.Show("Could not load mipmap " & MipmapIndex.ToString() & ":" & Environment.NewLine & ex.Message, "BLP Orrery", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadCurrentIcoMipmap(MipmapIndex As Integer)
        If String.IsNullOrWhiteSpace(CurrentFilePath) OrElse Not IsIcoFile(CurrentFilePath) Then Return

        Try
            Using fileStream As New FileStream(CurrentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Dim ICO As New IcoFile(fileStream)
                If ICO.ImageCount <= 0 Then Return

                If MipmapIndex < 0 Then MipmapIndex = 0
                If MipmapIndex >= ICO.ImageCount Then MipmapIndex = ICO.ImageCount - 1

                CurrentMipMapIndex = MipmapIndex
                SetSourceBitmap(ICO.GetBitmap(CurrentMipMapIndex))
                SetActiveIcoInfo(ICO, CurrentMipMapIndex)
            End Using
        Catch ex As Exception
            MessageBox.Show("Could not load icon image " & MipmapIndex.ToString() & ":" & Environment.NewLine & ex.Message, "BLP Orrery", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ResetFileInfo()
        LsBxMipMapList.Items.Clear()
        LsBxMipMapList.Items.Add(" # | Size      | Bytes    | Offset")
        LblCompressionValue.Text = "N/A"
        LblAlphaEncodingValue.Text = "N/A"
        LblAlphaChannelValue.Text = "N/A"
        LblResolutionValue.Text = "N/A"
        LblMipMapCountValue.Text = "N/A"
        LblActiveMipMaipValue.Text = "N/A"
    End Sub

    Private Sub SetActiveMipmapInfo(MipmapIndex As Integer, Width As Integer, Height As Integer)
        LblActiveMipMaipValue.Text = MipmapIndex.ToString()
        LblResolutionValue.Text = Width.ToString() & " x " & Height.ToString()
        UpdateTextureViewSizing(Width, Height)
    End Sub

    Private Sub SetSourceBitmap(BitmapToDisplay As Bitmap)
        Dim nextBitmap As Bitmap = BitmapToDisplay

        If BitmapToDisplay.PixelFormat <> PixelFormat.Format32bppArgb Then
            nextBitmap = CloneAs32BppArgb(BitmapToDisplay)
            BitmapToDisplay.Dispose()
        End If

        If CurrentSourceBitmap IsNot Nothing Then CurrentSourceBitmap.Dispose()
        CurrentSourceBitmap = nextBitmap
        RenderCurrentPreview()
    End Sub

    Private Sub RenderCurrentPreview()
        If CurrentSourceBitmap Is Nothing Then Return

        Dim preview As Bitmap
        If MaskModeEnabled Then
            preview = CreateAlphaMaskBitmap(CurrentSourceBitmap)
        Else
            preview = ComposePreviewBitmap(CurrentSourceBitmap)
        End If

        SetPreviewImage(preview)
        UpdateDisplayButtons()
    End Sub

    Private Function ComposePreviewBitmap(Source As Bitmap) As Bitmap
        If Not RenderTransparencyMenuItem.Checked Then
            Return CreateOpaqueRgbBitmap(Source)
        End If

        Dim preview As New Bitmap(Source.Width, Source.Height, PixelFormat.Format32bppArgb)

        Using graphics As Graphics = Graphics.FromImage(preview)
            graphics.CompositingMode = CompositingMode.SourceOver
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor
            graphics.PixelOffsetMode = PixelOffsetMode.Half
            graphics.Clear(Color.Transparent)
            graphics.DrawImage(Source, New Rectangle(0, 0, Source.Width, Source.Height))
        End Using

        Return preview
    End Function

    Private Function CreateOpaqueRgbBitmap(Source As Bitmap) As Bitmap
        Dim preview As New Bitmap(Source.Width, Source.Height, PixelFormat.Format32bppArgb)
        Dim rect As New Rectangle(0, 0, Source.Width, Source.Height)
        Dim sourceData As BitmapData = Nothing
        Dim previewData As BitmapData = Nothing
        Dim backgroundRed As Integer = TransparencyBackgroundColor.R
        Dim backgroundGreen As Integer = TransparencyBackgroundColor.G
        Dim backgroundBlue As Integer = TransparencyBackgroundColor.B

        Try
            sourceData = Source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
            previewData = preview.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb)

            Dim sourceStride As Integer = Math.Abs(sourceData.Stride)
            Dim previewStride As Integer = Math.Abs(previewData.Stride)
            Dim sourceBytes As Byte() = New Byte(sourceStride * Source.Height - 1) {}
            Dim previewBytes As Byte() = New Byte(previewStride * Source.Height - 1) {}

            Marshal.Copy(sourceData.Scan0, sourceBytes, 0, sourceBytes.Length)

            For y As Integer = 0 To Source.Height - 1
                Dim sourceRow As Integer = y * sourceStride
                Dim previewRow As Integer = y * previewStride

                For x As Integer = 0 To Source.Width - 1
                    Dim sourceOffset As Integer = sourceRow + x * 4
                    Dim previewOffset As Integer = previewRow + x * 4
                    Dim alpha As Integer = sourceBytes(sourceOffset + 3)
                    Dim inverseAlpha As Integer = 255 - alpha

                    previewBytes(previewOffset) = CByte(((CInt(sourceBytes(sourceOffset)) * alpha) + (backgroundBlue * inverseAlpha) + 127) \ 255)
                    previewBytes(previewOffset + 1) = CByte(((CInt(sourceBytes(sourceOffset + 1)) * alpha) + (backgroundGreen * inverseAlpha) + 127) \ 255)
                    previewBytes(previewOffset + 2) = CByte(((CInt(sourceBytes(sourceOffset + 2)) * alpha) + (backgroundRed * inverseAlpha) + 127) \ 255)
                    previewBytes(previewOffset + 3) = 255
                Next
            Next

            Marshal.Copy(previewBytes, 0, previewData.Scan0, previewBytes.Length)
        Finally
            If sourceData IsNot Nothing Then Source.UnlockBits(sourceData)
            If previewData IsNot Nothing Then preview.UnlockBits(previewData)
        End Try

        Return preview
    End Function

    Private Function CreateAlphaMaskBitmap(Source As Bitmap) As Bitmap
        Dim mask As New Bitmap(Source.Width, Source.Height, PixelFormat.Format32bppArgb)
        Dim rect As New Rectangle(0, 0, Source.Width, Source.Height)
        Dim sourceData As BitmapData = Nothing
        Dim maskData As BitmapData = Nothing

        Try
            sourceData = Source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
            maskData = mask.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb)

            Dim sourceStride As Integer = Math.Abs(sourceData.Stride)
            Dim maskStride As Integer = Math.Abs(maskData.Stride)
            Dim sourceBytes As Byte() = New Byte(sourceStride * Source.Height - 1) {}
            Dim maskBytes As Byte() = New Byte(maskStride * Source.Height - 1) {}

            Marshal.Copy(sourceData.Scan0, sourceBytes, 0, sourceBytes.Length)

            For y As Integer = 0 To Source.Height - 1
                Dim sourceRow As Integer = y * sourceStride
                Dim maskRow As Integer = y * maskStride

                For x As Integer = 0 To Source.Width - 1
                    Dim sourceOffset As Integer = sourceRow + x * 4
                    Dim maskOffset As Integer = maskRow + x * 4
                    Dim maskValue As Byte = CByte(255 - sourceBytes(sourceOffset + 3))

                    maskBytes(maskOffset) = maskValue
                    maskBytes(maskOffset + 1) = maskValue
                    maskBytes(maskOffset + 2) = maskValue
                    maskBytes(maskOffset + 3) = 255
                Next
            Next

            Marshal.Copy(maskBytes, 0, maskData.Scan0, maskBytes.Length)
        Finally
            If sourceData IsNot Nothing Then Source.UnlockBits(sourceData)
            If maskData IsNot Nothing Then mask.UnlockBits(maskData)
        End Try

        Return mask
    End Function

    Private Function CloneAs32BppArgb(Source As Image) As Bitmap
        Dim clone As New Bitmap(Source.Width, Source.Height, PixelFormat.Format32bppArgb)
        Using graphics As Graphics = Graphics.FromImage(clone)
            graphics.Clear(Color.Transparent)
            graphics.DrawImage(Source, New Rectangle(0, 0, Source.Width, Source.Height))
        End Using
        Return clone
    End Function

    Private Sub SetPreviewImage(ImageToDisplay As Image)
        Dim oldImage As Image = PicBxTextureView.Image
        PicBxTextureView.Image = ImageToDisplay
        ApplyPreviewZoomLayout()
        If oldImage IsNot Nothing AndAlso Not Object.ReferenceEquals(oldImage, ImageToDisplay) Then oldImage.Dispose()
    End Sub

    Private Sub DisposeCurrentImages()
        If CurrentSourceBitmap IsNot Nothing Then
            CurrentSourceBitmap.Dispose()
            CurrentSourceBitmap = Nothing
        End If

        If PicBxTextureView.Image IsNot Nothing Then
            PicBxTextureView.Image.Dispose()
            PicBxTextureView.Image = Nothing
        End If
    End Sub

    Private Sub TxtbxWoWPath_DoubleClick(sender As Object, e As EventArgs) Handles TxBxTextureDirectory.DoubleClick
        If Directory.Exists(TxBxTextureDirectory.Text) Then Process.Start("explorer.exe", TxBxTextureDirectory.Text)
    End Sub

    Private Sub OpenFileToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OpenFileToolStripMenuItem.Click
        OpenImage_OFD1_Click(Nothing, Nothing)
    End Sub

    Private Sub QuitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles QuitToolStripMenuItem.Click
        Close()
    End Sub

    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        ShowAboutWindow()
    End Sub

    Private Sub MainContextAboutItem_Click(sender As Object, e As EventArgs)
        ShowAboutWindow()
    End Sub

    Private Sub BtnTransparency_Click(sender As Object, e As EventArgs) Handles BtnTransparency.Click
        RenderTransparencyMenuItem.Checked = Not RenderTransparencyMenuItem.Checked
    End Sub

    Private Sub BtnMask_Click(sender As Object, e As EventArgs) Handles BtnMask.Click
        MaskModeEnabled = Not MaskModeEnabled
        RenderCurrentPreview()
    End Sub

    Private Sub RenderTransparencyMenuItem_CheckedChanged(sender As Object, e As EventArgs) Handles RenderTransparencyMenuItem.CheckedChanged
        If Not IsLoadingImage Then RenderCurrentPreview()
        UpdateDisplayButtons()
    End Sub

    Private Sub FileInformationsToolStripMenuItem_CheckedChanged(sender As Object, e As EventArgs) Handles FileInformationsToolStripMenuItem.CheckedChanged
        PnlFileInfo.Visible = FileInformationsToolStripMenuItem.Checked
        If Not IsLoadingImage Then UpdateTextureViewSizing()
    End Sub

    Private Sub ResizeByTextureMenuItem_CheckedChanged(sender As Object, e As EventArgs) Handles ResizeByTextureMenuItem.CheckedChanged
        If ResizeByTextureMenuItem.Checked Then
            PreviewZoomIsManual = True
            PreviewZoomFactor = 1.0F
        Else
            PreviewZoomIsManual = False
            PreviewZoomFactor = 1.0F
        End If

        UpdateTextureViewSizing()
    End Sub

    Private Sub BackgroundClrMenuItem_Click(sender As Object, e As EventArgs) Handles BackgroundClrMenuItem.Click
        ClrPickBackGround.Color = TransparencyBackgroundColor

        If ClrPickBackGround.ShowDialog = System.Windows.Forms.DialogResult.OK Then
            SetTransparencyBackgroundColor(ClrPickBackGround.Color)
        End If
    End Sub

    Private Sub LsBxMipMapList_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LsBxMipMapList.SelectedIndexChanged
        If IsLoadingImage Then Return
        If LsBxMipMapList.SelectedIndex > 0 Then
            If IsBlpFile(CurrentFilePath) Then
                LoadCurrentBlpMipmap(LsBxMipMapList.SelectedIndex - 1)
            ElseIf IsDdsFile(CurrentFilePath) Then
                LoadCurrentDdsMipmap(LsBxMipMapList.SelectedIndex - 1)
            ElseIf IsIcoFile(CurrentFilePath) Then
                LoadCurrentIcoMipmap(LsBxMipMapList.SelectedIndex - 1)
            End If
        End If
    End Sub

    Private Sub BtnPreviousImage_Click(sender As Object, e As EventArgs) Handles BtnPreviousImage.Click
        QueueSiblingImageNavigation(-1)
    End Sub

    Private Sub BtnNextImage_Click(sender As Object, e As EventArgs) Handles BtnNextImage.Click
        QueueSiblingImageNavigation(1)
    End Sub

    Private Sub BtnZoomOut_Click(sender As Object, e As EventArgs) Handles BtnZoomOut.Click
        ZoomPreview(False, GetPreviewCenterPoint())
    End Sub

    Private Sub BtnZoomIn_Click(sender As Object, e As EventArgs) Handles BtnZoomIn.Click
        ZoomPreview(True, GetPreviewCenterPoint())
    End Sub

    Private Sub PnlToolBar_Resize(sender As Object, e As EventArgs) Handles PnlToolBar.Resize
        PositionNavigationButtons()
    End Sub

    Private Sub PnlTextureView_Resize(sender As Object, e As EventArgs) Handles PnlTextureView.Resize
        ApplyPreviewZoomLayout()
    End Sub

    Public Function PreFilterMessage(ByRef m As Message) As Boolean Implements IMessageFilter.PreFilterMessage
        If m.Msg <> WM_MOUSEWHEEL OrElse CurrentSourceBitmap Is Nothing Then Return False
        If Not ContainsFocus AndAlso Not Object.ReferenceEquals(Form.ActiveForm, Me) Then Return False

        Dim mouseScreenPoint As Point = Control.MousePosition
        If Not PnlTextureView.RectangleToScreen(PnlTextureView.ClientRectangle).Contains(mouseScreenPoint) Then Return False

        Dim wheelDelta As Integer = CInt((m.WParam.ToInt64() >> 16) And &HFFFF)
        If wheelDelta >= &H8000 Then wheelDelta -= &H10000
        If wheelDelta = 0 Then Return False

        ZoomPreview(wheelDelta > 0, PnlTextureView.PointToClient(mouseScreenPoint))
        Return True
    End Function

    Private Sub EnableMainContextMenu()
        If MainContextMenu IsNot Nothing Then Return

        If components Is Nothing Then components = New System.ComponentModel.Container()

        MainContextMenu = New ContextMenuStrip(components)
        Dim aboutItem As New ToolStripMenuItem("About")
        AddHandler aboutItem.Click, AddressOf MainContextAboutItem_Click
        MainContextMenu.Items.Add(aboutItem)

        Dim targets As Control() = {Me, PnlTextureViewAndToolBar, PnlTextureView, PicBxTextureView, PnlToolBar, PnlFileInfo, GrBxFileInfo}
        For Each target As Control In targets
            target.ContextMenuStrip = MainContextMenu
        Next
    End Sub

    Private Sub EnableMainFrameDragDrop()
        Dim targets As Control() = {Me, PnlTextureViewAndToolBar, PnlTextureView, PicBxTextureView, PnlToolBar, PnlFileInfo, GrBxFileInfo}

        For Each target As Control In targets
            target.AllowDrop = True
            AddHandler target.DragEnter, AddressOf MainFrame_DragEnter
            AddHandler target.DragDrop, AddressOf MainFrame_DragDrop
        Next
    End Sub

    Private Sub MainFrame_DragEnter(sender As Object, e As DragEventArgs)
        e.Effect = DragDropEffects.None

        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            Dim files As String() = CType(e.Data.GetData(DataFormats.FileDrop), String())
            If GetFirstSupportedFile(files) IsNot Nothing Then e.Effect = DragDropEffects.Copy
        End If
    End Sub

    Private Sub MainFrame_DragDrop(sender As Object, e As DragEventArgs)
        If Not e.Data.GetDataPresent(DataFormats.FileDrop) Then Return

        Dim files As String() = CType(e.Data.GetData(DataFormats.FileDrop), String())
        Dim fileToOpen As String = GetFirstSupportedFile(files)
        If fileToOpen IsNot Nothing Then OpenImageFile(fileToOpen)
    End Sub

    Private Function GetFirstSupportedFile(Files As String()) As String
        If Files Is Nothing Then Return Nothing

        For Each filePath As String In Files
            If File.Exists(filePath) AndAlso IsSupportedImageFile(filePath) Then Return filePath

            If Directory.Exists(filePath) Then
                For Each childFile As String In Directory.GetFiles(filePath)
                    If IsSupportedImageFile(childFile) Then Return childFile
                Next
            End If
        Next

        Return Nothing
    End Function

    Private Sub QueueSiblingImageNavigation(Direction As Integer)
        If Direction = 0 Then Return

        Dim imageFiles As String() = GetSiblingImageFiles()
        If imageFiles.Length <= 1 Then Return

        Dim baseIndex As Integer = -1
        If PendingNavigationIndex >= 0 AndAlso PendingNavigationIndex < imageFiles.Length Then
            If imageFiles(PendingNavigationIndex).Equals(PendingNavigationFilePath, StringComparison.OrdinalIgnoreCase) Then
                baseIndex = PendingNavigationIndex
            End If
        End If

        If baseIndex < 0 Then
            baseIndex = Array.FindIndex(imageFiles, Function(path) path.Equals(CurrentFilePath, StringComparison.OrdinalIgnoreCase))
            If baseIndex < 0 Then baseIndex = 0
        End If

        PendingNavigationIndex = (baseIndex + Direction + imageFiles.Length) Mod imageFiles.Length
        PendingNavigationFilePath = imageFiles(PendingNavigationIndex)

        EnsureNavigationTimer()
        NavigationTimer.Stop()
        NavigationTimer.Start()
    End Sub

    Private Sub NavigationTimer_Tick(sender As Object, e As EventArgs)
        CommitQueuedSiblingNavigation()
    End Sub

    Private Sub CommitQueuedSiblingNavigation()
        If NavigationTimer IsNot Nothing Then NavigationTimer.Stop()

        Dim targetPath As String = PendingNavigationFilePath
        PendingNavigationFilePath = String.Empty
        PendingNavigationIndex = -1

        If String.IsNullOrWhiteSpace(targetPath) Then Return
        If targetPath.Equals(CurrentFilePath, StringComparison.OrdinalIgnoreCase) Then Return

        OpenImageFile(targetPath, True)
    End Sub

    Private Sub CancelQueuedSiblingNavigation()
        If NavigationTimer IsNot Nothing Then NavigationTimer.Stop()
        PendingNavigationFilePath = String.Empty
        PendingNavigationIndex = -1
    End Sub

    Private Sub EnsureNavigationTimer()
        If NavigationTimer IsNot Nothing Then Return

        NavigationTimer = New Timer()
        NavigationTimer.Interval = NavigationCoalesceIntervalMs
        AddHandler NavigationTimer.Tick, AddressOf NavigationTimer_Tick
    End Sub

    Private Sub DisposeNavigationTimer()
        If NavigationTimer Is Nothing Then Return

        NavigationTimer.Stop()
        RemoveHandler NavigationTimer.Tick, AddressOf NavigationTimer_Tick
        NavigationTimer.Dispose()
        NavigationTimer = Nothing
    End Sub

    Private Function CanNavigateSiblingImages() As Boolean
        Return GetSiblingImageFiles().Length > 1
    End Function

    Private Function GetSiblingImageFiles() As String()
        If String.IsNullOrWhiteSpace(CurrentFilePath) OrElse Not File.Exists(CurrentFilePath) Then Return New String() {}

        Dim directoryPath As String = Path.GetDirectoryName(CurrentFilePath)
        If Not Directory.Exists(directoryPath) Then Return New String() {}

        Dim directoryWriteTimeUtc As DateTime
        Try
            directoryWriteTimeUtc = Directory.GetLastWriteTimeUtc(directoryPath)
        Catch
            Return New String() {}
        End Try

        If directoryPath.Equals(SiblingImageCacheDirectory, StringComparison.OrdinalIgnoreCase) AndAlso
           directoryWriteTimeUtc = SiblingImageCacheLastWriteUtc Then
            Return SiblingImageCacheFiles
        End If

        Dim imageFiles As New List(Of String)
        For Each filePath As String In Directory.GetFiles(directoryPath)
            If IsSupportedImageFile(filePath) Then imageFiles.Add(Path.GetFullPath(filePath))
        Next

        imageFiles.Sort(StringComparer.CurrentCultureIgnoreCase)
        SiblingImageCacheDirectory = directoryPath
        SiblingImageCacheLastWriteUtc = directoryWriteTimeUtc
        SiblingImageCacheFiles = imageFiles.ToArray()

        Return SiblingImageCacheFiles
    End Function

    Private Sub PositionNavigationButtons()
        If BtnPreviousImage Is Nothing OrElse BtnNextImage Is Nothing OrElse BtnZoomOut Is Nothing OrElse BtnZoomIn Is Nothing OrElse PnlToolBar Is Nothing Then Return

        Dim buttonGap As Integer = 6
        Dim buttonLeft As Integer = 4
        Dim buttonTop As Integer = Math.Max(4, PnlToolBar.ClientSize.Height - BtnPreviousImage.Height - 4)

        BtnPreviousImage.Location = New Point(buttonLeft, buttonTop)
        BtnNextImage.Location = New Point(buttonLeft + BtnPreviousImage.Width + buttonGap, buttonTop)
        BtnZoomOut.Location = New Point(BtnNextImage.Right + buttonGap, buttonTop)
        BtnZoomIn.Location = New Point(BtnZoomOut.Right + buttonGap, buttonTop)
        BtnPreviousImage.BringToFront()
        BtnNextImage.BringToFront()
        BtnZoomOut.BringToFront()
        BtnZoomIn.BringToFront()
    End Sub

    Private Sub UpdateNavigationButtons()
        If BtnPreviousImage Is Nothing OrElse BtnNextImage Is Nothing Then Return

        Dim hasSiblingImages As Boolean = CanNavigateSiblingImages()
        BtnPreviousImage.Enabled = hasSiblingImages
        BtnNextImage.Enabled = hasSiblingImages
    End Sub

    Private Sub ResetPreviewZoomForNewImage()
        PreviewZoomFactor = 1.0F
        PreviewZoomIsManual = ResizeByTextureMenuItem IsNot Nothing AndAlso ResizeByTextureMenuItem.Checked
    End Sub

    Private Sub UpdateTextureViewSizing(Optional TextureWidth As Integer = 0, Optional TextureHeight As Integer = 0)
        If PicBxTextureView Is Nothing OrElse ResizeByTextureMenuItem Is Nothing Then Return

        If ResizeByTextureMenuItem.Checked Then
            PreviewZoomIsManual = True
            PreviewZoomFactor = 1.0F
        End If

        ApplyPreviewZoomLayout()

        If Not ResizeByTextureMenuItem.Checked Then
            Return
        End If

        If TextureWidth <= 0 OrElse TextureHeight <= 0 Then
            If CurrentSourceBitmap Is Nothing Then Return
            TextureWidth = CurrentSourceBitmap.Width
            TextureHeight = CurrentSourceBitmap.Height
        End If

        ResizeWindowForTexture(TextureWidth, TextureHeight)
    End Sub

    Private Sub ResizeWindowForTexture(TextureWidth As Integer, TextureHeight As Integer)
        If TextureWidth <= 0 OrElse TextureHeight <= 0 Then Return

        If WindowState <> FormWindowState.Normal Then WindowState = FormWindowState.Normal
        PerformLayout()

        Dim targetPreviewWidth As Integer = Math.Max(TextureWidth, 320)
        Dim targetPreviewHeight As Integer = Math.Max(TextureHeight, 128)
        Dim widthDelta As Integer = targetPreviewWidth - PicBxTextureView.ClientSize.Width
        Dim heightDelta As Integer = targetPreviewHeight - PicBxTextureView.ClientSize.Height

        If widthDelta = 0 AndAlso heightDelta = 0 Then Return

        Dim targetClientWidth As Integer = Math.Max(1, ClientSize.Width + widthDelta)
        Dim targetClientHeight As Integer = Math.Max(1, ClientSize.Height + heightDelta)

        ClientSize = New Size(targetClientWidth, targetClientHeight)
        PerformLayout()
        PositionNavigationButtons()
    End Sub

    Private Sub UpdateDisplayButtons()
        If BtnMask IsNot Nothing Then BtnMask.BackColor = If(MaskModeEnabled, Color.LightSteelBlue, Color.Silver)
        If BtnTransparency IsNot Nothing Then BtnTransparency.BackColor = If(RenderTransparencyMenuItem.Checked, Color.LightSteelBlue, Color.Silver)

        Dim hasImage As Boolean = CurrentSourceBitmap IsNot Nothing
        If BtnZoomOut IsNot Nothing Then BtnZoomOut.Enabled = hasImage
        If BtnZoomIn IsNot Nothing Then BtnZoomIn.Enabled = hasImage
    End Sub

    Private Sub ZoomPreview(ZoomIn As Boolean, AnchorPoint As Point)
        If PicBxTextureView.Image Is Nothing Then Return

        Dim oldZoomFactor As Single = If(PreviewZoomIsManual, PreviewZoomFactor, GetFitZoomFactor())
        Dim imageX As Single = 0.0F
        Dim imageY As Single = 0.0F

        If oldZoomFactor > 0.0F Then
            imageX = CSng((GetHorizontalScrollOffset() + AnchorPoint.X - PicBxTextureView.Left) / oldZoomFactor)
            imageY = CSng((GetVerticalScrollOffset() + AnchorPoint.Y - PicBxTextureView.Top) / oldZoomFactor)
        End If

        PreviewZoomIsManual = True
        If ZoomIn Then
            PreviewZoomFactor = ClampZoomFactor(oldZoomFactor * PreviewZoomStep)
        Else
            PreviewZoomFactor = ClampZoomFactor(oldZoomFactor / PreviewZoomStep)
        End If

        ApplyPreviewZoomLayout()
        ScrollPreviewToAnchor(imageX, imageY, AnchorPoint)
        UpdateDisplayButtons()
    End Sub

    Private Function ClampZoomFactor(ZoomFactor As Single) As Single
        If ZoomFactor < MinPreviewZoomFactor Then Return MinPreviewZoomFactor
        If ZoomFactor > MaxPreviewZoomFactor Then Return MaxPreviewZoomFactor
        Return ZoomFactor
    End Function

    Private Function GetFitZoomFactor() As Single
        If PicBxTextureView.Image Is Nothing OrElse PicBxTextureView.Image.Width <= 0 OrElse PicBxTextureView.Image.Height <= 0 Then Return 1.0F
        If PnlTextureView.ClientSize.Width <= 0 OrElse PnlTextureView.ClientSize.Height <= 0 Then Return 1.0F

        Dim widthScale As Single = CSng(PnlTextureView.ClientSize.Width / CDbl(PicBxTextureView.Image.Width))
        Dim heightScale As Single = CSng(PnlTextureView.ClientSize.Height / CDbl(PicBxTextureView.Image.Height))
        Return ClampZoomFactor(Math.Min(widthScale, heightScale))
    End Function

    Private Function GetPreviewCenterPoint() As Point
        Return New Point(Math.Max(0, PnlTextureView.ClientSize.Width \ 2), Math.Max(0, PnlTextureView.ClientSize.Height \ 2))
    End Function

    Private Function GetHorizontalScrollOffset() As Integer
        If PnlTextureView.HorizontalScroll.Visible Then Return PnlTextureView.HorizontalScroll.Value
        Return 0
    End Function

    Private Function GetVerticalScrollOffset() As Integer
        If PnlTextureView.VerticalScroll.Visible Then Return PnlTextureView.VerticalScroll.Value
        Return 0
    End Function

    Private Sub ScrollPreviewToAnchor(ImageX As Single, ImageY As Single, AnchorPoint As Point)
        Dim desiredX As Integer = Math.Max(0, CInt(Math.Round(ImageX * PreviewZoomFactor + PicBxTextureView.Left - AnchorPoint.X)))
        Dim desiredY As Integer = Math.Max(0, CInt(Math.Round(ImageY * PreviewZoomFactor + PicBxTextureView.Top - AnchorPoint.Y)))

        PnlTextureView.AutoScrollPosition = New Point(desiredX, desiredY)
    End Sub

    Private Sub ApplyPreviewZoomLayout()
        If PicBxTextureView Is Nothing OrElse PnlTextureView Is Nothing Then Return

        If PicBxTextureView.Image Is Nothing Then
            PnlTextureView.AutoScroll = False
            PicBxTextureView.Dock = DockStyle.Fill
            PicBxTextureView.SizeMode = PictureBoxSizeMode.Zoom
            Return
        End If

        If Not PreviewZoomIsManual Then
            PnlTextureView.AutoScroll = False
            PicBxTextureView.Dock = DockStyle.Fill
            PicBxTextureView.SizeMode = PictureBoxSizeMode.Zoom
            Return
        End If

        PnlTextureView.AutoScroll = True
        PicBxTextureView.Dock = DockStyle.None
        PicBxTextureView.SizeMode = PictureBoxSizeMode.StretchImage

        Dim targetWidth As Integer = Math.Max(1, CInt(Math.Round(PicBxTextureView.Image.Width * CDbl(PreviewZoomFactor))))
        Dim targetHeight As Integer = Math.Max(1, CInt(Math.Round(PicBxTextureView.Image.Height * CDbl(PreviewZoomFactor))))
        Dim targetLeft As Integer = If(targetWidth < PnlTextureView.ClientSize.Width, (PnlTextureView.ClientSize.Width - targetWidth) \ 2, 0)
        Dim targetTop As Integer = If(targetHeight < PnlTextureView.ClientSize.Height, (PnlTextureView.ClientSize.Height - targetHeight) \ 2, 0)

        PicBxTextureView.Bounds = New Rectangle(targetLeft, targetTop, targetWidth, targetHeight)
    End Sub

    Private Sub SetTransparencyBackgroundColor(SelectedColor As Color, Optional RefreshPreview As Boolean = True)
        TransparencyBackgroundColor = Color.FromArgb(255, SelectedColor.R, SelectedColor.G, SelectedColor.B)
        ClrPickBackGround.Color = TransparencyBackgroundColor
        BackgroundClrMenuItem.ForeColor = TransparencyBackgroundColor

        If RefreshPreview AndAlso Not IsLoadingImage Then RenderCurrentPreview()
    End Sub

    Private Sub UpdateWindowTitle(Optional FileName As String = Nothing)
        If String.IsNullOrWhiteSpace(FileName) Then
            Text = ApplicationTitle
            Return
        End If

        Text = ApplicationTitle & " - " & Path.GetFileName(FileName)
    End Sub

    Private Sub ShowAboutWindow()
        Using aboutWindow As Form = CreateAboutWindow()
            aboutWindow.ShowDialog(Me)
        End Using
    End Sub

    Private Function CreateAboutWindow() As Form
        Dim aboutWindow As New Form()
        aboutWindow.Text = "About " & ApplicationTitle
        aboutWindow.StartPosition = FormStartPosition.CenterParent
        aboutWindow.FormBorderStyle = FormBorderStyle.Sizable
        aboutWindow.MinimizeBox = False
        aboutWindow.ShowInTaskbar = False
        aboutWindow.ClientSize = New Size(860, 600)
        aboutWindow.MinimumSize = New Size(700, 500)
        aboutWindow.BackColor = Color.FromArgb(246, 247, 249)
        If Icon IsNot Nothing Then aboutWindow.Icon = Icon

        Dim splashImage As Image = LoadSplashArtImage()
        AddHandler aboutWindow.FormClosed,
            Sub()
                If splashImage IsNot Nothing Then splashImage.Dispose()
            End Sub

        Dim rootLayout As New TableLayoutPanel()
        rootLayout.Dock = DockStyle.Fill
        rootLayout.Padding = New Padding(12)
        rootLayout.ColumnCount = 2
        rootLayout.RowCount = 2
        rootLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 330.0F))
        rootLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        rootLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        rootLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 44.0F))

        Dim splashBox As New PictureBox()
        splashBox.Dock = DockStyle.Fill
        splashBox.BackColor = Color.FromArgb(18, 20, 24)
        splashBox.BorderStyle = BorderStyle.FixedSingle
        splashBox.SizeMode = PictureBoxSizeMode.Zoom
        splashBox.Image = splashImage
        rootLayout.Controls.Add(splashBox, 0, 0)

        Dim infoLayout As New TableLayoutPanel()
        infoLayout.Dock = DockStyle.Fill
        infoLayout.ColumnCount = 1
        infoLayout.RowCount = 6
        infoLayout.Padding = New Padding(10, 0, 0, 0)
        infoLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        infoLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        infoLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        infoLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        infoLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        infoLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        Dim titleLabel As New Label()
        titleLabel.AutoSize = True
        titleLabel.Font = New Font(Font.FontFamily, 18.0F, FontStyle.Bold)
        titleLabel.ForeColor = Color.FromArgb(28, 32, 38)
        titleLabel.Text = ApplicationTitle & " " & CurrentVersion

        Dim authorLabel As New Label()
        authorLabel.AutoSize = True
        authorLabel.Margin = New Padding(0, 8, 0, 0)
        authorLabel.Font = New Font(Font.FontFamily, 9.5F, FontStyle.Regular)
        authorLabel.Text = "Author: " & AuthorName & Environment.NewLine &
                           "Development begun: " & DevelopmentStartYear

        Dim formatsHeader As New Label()
        formatsHeader.AutoSize = True
        formatsHeader.Margin = New Padding(0, 16, 0, 4)
        formatsHeader.Font = New Font(Font.FontFamily, 10.0F, FontStyle.Bold)
        formatsHeader.Text = "Supported formats"

        Dim formatsLabel As New Label()
        formatsLabel.AutoSize = True
        formatsLabel.Font = New Font(Font.FontFamily, 9.0F, FontStyle.Regular)
        formatsLabel.Text = String.Join(Environment.NewLine, New String() {
            "BLP - Blizzard Picture, including RAW3",
            "DDS - DirectDraw Surface, including BioWare/NWN compact DDS",
            "TGA - Truevision TGA",
            "ICO - Windows Icon",
            "PNG - Portable Network Graphics",
            "JPG/JPEG - JPEG images"
        })

        Dim changelogHeader As New Label()
        changelogHeader.AutoSize = True
        changelogHeader.Margin = New Padding(0, 16, 0, 4)
        changelogHeader.Font = New Font(Font.FontFamily, 10.0F, FontStyle.Bold)
        changelogHeader.Text = "Changelog"

        Dim changelogBox As New TextBox()
        changelogBox.Dock = DockStyle.Fill
        changelogBox.Multiline = True
        changelogBox.ReadOnly = True
        changelogBox.ScrollBars = ScrollBars.Vertical
        changelogBox.BackColor = Color.White
        changelogBox.BorderStyle = BorderStyle.FixedSingle
        changelogBox.Font = New Font("Consolas", 8.75F, FontStyle.Regular)
        changelogBox.Text = GetAboutChangelogText()

        infoLayout.Controls.Add(titleLabel, 0, 0)
        infoLayout.Controls.Add(authorLabel, 0, 1)
        infoLayout.Controls.Add(formatsHeader, 0, 2)
        infoLayout.Controls.Add(formatsLabel, 0, 3)
        infoLayout.Controls.Add(changelogHeader, 0, 4)
        infoLayout.Controls.Add(changelogBox, 0, 5)

        rootLayout.Controls.Add(infoLayout, 1, 0)

        Dim okButton As New Button()
        okButton.Text = "OK"
        okButton.DialogResult = DialogResult.OK
        okButton.Anchor = AnchorStyles.Right Or AnchorStyles.Bottom
        okButton.Size = New Size(92, 28)

        Dim buttonPanel As New FlowLayoutPanel()
        buttonPanel.Dock = DockStyle.Fill
        buttonPanel.FlowDirection = FlowDirection.RightToLeft
        buttonPanel.WrapContents = False
        buttonPanel.Padding = New Padding(0, 8, 0, 0)
        buttonPanel.Controls.Add(okButton)

        rootLayout.Controls.Add(buttonPanel, 1, 1)
        aboutWindow.AcceptButton = okButton
        aboutWindow.CancelButton = okButton
        aboutWindow.Controls.Add(rootLayout)

        Return aboutWindow
    End Function

    Private Function LoadSplashArtImage() As Image
        Dim splashPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SplashArtRelativePath)
        If File.Exists(splashPath) Then Return Image.FromFile(splashPath)

        splashPath = Path.Combine(Application.StartupPath, SplashArtRelativePath)
        If File.Exists(splashPath) Then Return Image.FromFile(splashPath)

        Dim assemblyPath As String = GetType(BLP_Orrery_MainForm).Assembly.Location
        If Not String.IsNullOrWhiteSpace(assemblyPath) Then
            splashPath = Path.Combine(Path.GetDirectoryName(assemblyPath), SplashArtRelativePath)
            If File.Exists(splashPath) Then Return Image.FromFile(splashPath)
        End If

        Return Nothing
    End Function

    Private Function GetAboutChangelogText() As String
        Return String.Join(Environment.NewLine & Environment.NewLine, New String() {
            "0.7 - Baseline" & Environment.NewLine &
            "Inherited BLP Orrery as a BLP texture browser with file information and mipmap viewing.",
            "0.8 - Stability and BLP decoding" & Environment.NewLine &
            "Fixed compile/load errors, tightened file checks, improved DXT handling, and corrected mipmap preview artifacts.",
            "0.9 - Opening workflow" & Environment.NewLine &
            "Added drag and drop, command-line file launch, filename in the title bar, folder navigation buttons, and keyboard arrow browsing.",
            "1.0 - Preview modes" & Environment.NewLine &
            "Finished mask preview and transparency preview toggles, changed the default matte to black, and persisted transparency color settings.",
            "1.1 - View and layout settings" & Environment.NewLine &
            "Made File Informations visible by default, added the resize-by-texture option, and set the 585x430 minimum window size.",
            "1.2 - DDS engine" & Environment.NewLine &
            "Added standard DDS support, BioWare/NWN compact DDS support, and overflow protection for valid DDS images.",
            "1.3 - TGA and export" & Environment.NewLine &
            "Added TGA image support and completed Save As export for PNG and JPG while removing the unfinished BLP save option.",
            "1.4 - Windows Shell Extension Orrery" & Environment.NewLine &
            "Created the thumbnail-provider fork for Explorer previews of BLP, DDS, and BioWare/NWN compact DDS files.",
            "1.5 - RAW3 BLP support" & Environment.NewLine &
            "Added BLP encoding 3 RAW BGRA support and stronger malformed-file validation to avoid crashes.",
            "1.6 - Zoom controls" & Environment.NewLine &
            "Added zoom in/out toolbar buttons and mouse-wheel zoom for detailed texture inspection.",
            "1.7 - ICO support" & Environment.NewLine &
            "Added Windows icon viewing in Orrery and ICO thumbnail support in the shell extension.",
            "1.8 - About and metadata polish" & Environment.NewLine &
            "Added author data, development year, supported format list, context-menu About access, and persistent viewer preferences.",
            "1.9 - Splash art and project history" & Environment.NewLine &
            "Added thematic Orrery splash artwork and this versioned changelog to the About window."
        })
    End Function

    Private Function IsSupportedImageFile(FilePath As String) As Boolean
        Dim extension As String = Path.GetExtension(FilePath)
        For Each supportedExtension As String In SupportedImageExtensions
            If extension.Equals(supportedExtension, StringComparison.OrdinalIgnoreCase) Then Return True
        Next
        Return False
    End Function

    Private Function IsBlpFile(FilePath As String) As Boolean
        Return Path.GetExtension(FilePath).Equals(".BLP", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function IsDdsFile(FilePath As String) As Boolean
        Return Path.GetExtension(FilePath).Equals(".DDS", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function IsTgaFile(FilePath As String) As Boolean
        Return Path.GetExtension(FilePath).Equals(".TGA", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function IsIcoFile(FilePath As String) As Boolean
        Return Path.GetExtension(FilePath).Equals(".ICO", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function EnsureTrailingSlash(DirectoryPath As String) As String
        If String.IsNullOrWhiteSpace(DirectoryPath) Then Return String.Empty
        If DirectoryPath.EndsWith("\") Then Return DirectoryPath
        Return DirectoryPath & "\"
    End Function

End Class
