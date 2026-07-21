Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.InteropServices

Public Class BLP_Orrery_MainForm
    Implements IMessageFilter

    Private Const ApplicationTitle As String = "BLP Orrery"
    Private Const CurrentVersion As String = "2.9"
    Private Const AuthorName As String = "Alastor Strix'Efuartus"
    Private Const DevelopmentStartYear As String = "2022"
    Private Const RegistryPath As String = "HKEY_CURRENT_USER\WOWBLP_Orrery"
    Private Const SplashArtRelativePath As String = "Assets\OrrerySplashArt.png"
    Private Const MinPreviewZoomFactor As Single = 0.0625F
    Private Const MaxPreviewZoomFactor As Single = 32.0F
    Private Const PreviewZoomStep As Single = 1.25F
    Private Const NavigationCoalesceIntervalMs As Integer = 55
    Private Const MinimumRestoredWindowVisiblePixels As Integer = 64
    Private Const MaxTextureResizeDimension As Integer = 16384
    Private Const WM_MOUSEWHEEL As Integer = &H20A

    Private CurrentFilePath As String = String.Empty
    Private CurrentMipMapIndex As Integer = 0
    Private CurrentSourceBitmap As Bitmap = Nothing
    Private CurrentPltSelectedLayer As Integer = -1
    Private CurrentPltLayerIds As Integer() = New Integer() {}
    Private MaskModeEnabled As Boolean = False
    Private IsLoadingImage As Boolean = False
    Private MainContextMenu As ContextMenuStrip = Nothing
    Private TransparencyBackgroundColor As Color = Color.Black
    Private PreviewZoomFactor As Single = 1.0F
    Private PreviewZoomIsManual As Boolean = False
    Private IsPreviewPanning As Boolean = False
    Private PreviewPanStartMousePosition As Point = Point.Empty
    Private PreviewPanStartScrollPosition As Point = Point.Empty
    Private ResizeOverwriteOriginal As Boolean = False
    Private SiblingImageCacheDirectory As String = String.Empty
    Private SiblingImageCacheFiles As String() = New String() {}
    Private SiblingImageCacheLastWriteUtc As DateTime = DateTime.MinValue
    Private NavigationTimer As Timer = Nothing
    Private ToolbarToolTip As ToolTip = Nothing
    Private ShellExtensionStatus As OrreryShellStatus = Nothing
    Private IsUpdatingShellExtensionMenu As Boolean = False
    Private PendingNavigationFilePath As String = String.Empty
    Private PendingNavigationIndex As Integer = -1
    Private ReadOnly SupportedImageExtensions As String() = {".BLP", ".DDS", ".PLT", ".TGA", ".ICO", ".PNG", ".JPG", ".JPEG"}

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = (Keys.Control Or Keys.O) Then
            OpenFileToolStripMenuItem.PerformClick()
            Return True
        End If

        If keyData = (Keys.Control Or Keys.Shift Or Keys.S) Then
            SaveAsToolStripMenuItem.PerformClick()
            Return True
        End If

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
        DisposeToolbarToolTip()
        DisposeCurrentImages()
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Application.AddMessageFilter(Me)
        LoadApplicationSettings()
        EnsureWindowLocationIsVisible()
        ConfigureToolbarTooltips()
        UpdateWindowTitle()
        EnableMainContextMenu()
        EnableMainFrameDragDrop()
        PositionNavigationButtons()
        UpdateDisplayButtons()
        UpdateNavigationButtons()
        InitializeShellExtensionMenu()
        OpenStartupImageFromCommandLine(Environment.GetCommandLineArgs())
    End Sub

    Private Sub EnsureWindowLocationIsVisible()
        If IsWindowVisibleOnAnyScreen(Bounds) Then Return

        Dim workingArea As Rectangle = Screen.PrimaryScreen.WorkingArea
        Dim targetX As Integer = workingArea.Left + Math.Max(0, (workingArea.Width - Width) \ 2)
        Dim targetY As Integer = workingArea.Top + Math.Max(0, (workingArea.Height - Height) \ 2)

        StartPosition = FormStartPosition.Manual
        Location = New Point(targetX, targetY)
        My.Settings.WindowPoint = Location
    End Sub

    Private Function IsWindowVisibleOnAnyScreen(windowBounds As Rectangle) As Boolean
        For Each display As Screen In Screen.AllScreens
            Dim visibleArea As Rectangle = Rectangle.Intersect(windowBounds, display.WorkingArea)
            If visibleArea.Width >= MinimumRestoredWindowVisiblePixels AndAlso visibleArea.Height >= MinimumRestoredWindowVisiblePixels Then
                Return True
            End If
        Next

        Return False
    End Function

    Private Sub ConfigureToolbarTooltips()
        If ToolbarToolTip Is Nothing Then ToolbarToolTip = New ToolTip()
        If BtnResizePlt IsNot Nothing Then ToolbarToolTip.SetToolTip(BtnResizePlt, "Resize image resolution")
        If BtnZoomOut IsNot Nothing Then ToolbarToolTip.SetToolTip(BtnZoomOut, "Zoom out")
        If BtnZoomIn IsNot Nothing Then ToolbarToolTip.SetToolTip(BtnZoomIn, "Zoom in")
        If BtnPreviousImage IsNot Nothing Then ToolbarToolTip.SetToolTip(BtnPreviousImage, "Previous image")
        If BtnNextImage IsNot Nothing Then ToolbarToolTip.SetToolTip(BtnNextImage, "Next image")
    End Sub

    Private Sub DisposeToolbarToolTip()
        If ToolbarToolTip Is Nothing Then Return
        ToolbarToolTip.Dispose()
        ToolbarToolTip = Nothing
    End Sub

    Private Sub LoadApplicationSettings()
        FileInformationsToolStripMenuItem.CheckOnClick = True
        ResizeByTextureMenuItem.CheckOnClick = True
        PreviewActualSizeMenuItem.CheckOnClick = True
        FlipBioWareDdsMenuItem.CheckOnClick = True

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

        Dim savedPreviewActualSize As Object = My.Computer.Registry.GetValue(RegistryPath, "PreviewActualSize", Nothing)
        Dim previewActualSize As Boolean = False
        If savedPreviewActualSize IsNot Nothing Then
            Boolean.TryParse(savedPreviewActualSize.ToString(), previewActualSize)
        End If

        PreviewActualSizeMenuItem.Checked = previewActualSize

        Dim savedFlipBioWareDds As Object = My.Computer.Registry.GetValue(RegistryPath, "FlipBioWareDdsPreview", Nothing)
        Dim flipBioWareDds As Boolean = True
        If savedFlipBioWareDds IsNot Nothing Then
            Boolean.TryParse(savedFlipBioWareDds.ToString(), flipBioWareDds)
        End If

        FlipBioWareDdsMenuItem.Checked = flipBioWareDds

        Dim savedResizeOverwriteOriginal As Object = My.Computer.Registry.GetValue(RegistryPath, "ResizeOverwriteOriginal", Nothing)
        If savedResizeOverwriteOriginal Is Nothing Then savedResizeOverwriteOriginal = My.Computer.Registry.GetValue(RegistryPath, "PltResizeOverwriteOriginal", Nothing)
        ResizeOverwriteOriginal = False
        If savedResizeOverwriteOriginal IsNot Nothing Then
            Boolean.TryParse(savedResizeOverwriteOriginal.ToString(), ResizeOverwriteOriginal)
        End If

        UpdateTextureViewSizing()
    End Sub

    Private Sub SaveApplicationSettings()
        My.Computer.Registry.SetValue(RegistryPath, "SaveAsFormatIndex", ComBxSaveAsFormat.SelectedIndex)
        My.Computer.Registry.SetValue(RegistryPath, "RenderTransparency", RenderTransparencyMenuItem.Checked.ToString())
        My.Computer.Registry.SetValue(RegistryPath, "TransparencyBackgroundArgb", TransparencyBackgroundColor.ToArgb().ToString())
        My.Computer.Registry.SetValue(RegistryPath, "FileInformationVisible", FileInformationsToolStripMenuItem.Checked.ToString())
        My.Computer.Registry.SetValue(RegistryPath, "ResizeByTexture", ResizeByTextureMenuItem.Checked.ToString())
        My.Computer.Registry.SetValue(RegistryPath, "PreviewActualSize", PreviewActualSizeMenuItem.Checked.ToString())
        My.Computer.Registry.SetValue(RegistryPath, "FlipBioWareDdsPreview", FlipBioWareDdsMenuItem.Checked.ToString())
        My.Computer.Registry.SetValue(RegistryPath, "ResizeOverwriteOriginal", ResizeOverwriteOriginal.ToString())
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

    Private Sub ResizeImageResolution(sender As Object, e As EventArgs) Handles BtnResizePlt.Click, ResizePltToolStripMenuItem.Click
        If String.IsNullOrWhiteSpace(CurrentFilePath) OrElse Not File.Exists(CurrentFilePath) OrElse Not IsSupportedImageFile(CurrentFilePath) Then
            MessageBox.Show("Open a supported image before changing resolution.", ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Try
            Dim newWidth As Integer
            Dim newHeight As Integer
            Dim overwriteOriginal As Boolean = ResizeOverwriteOriginal

            If IsPltFile(CurrentFilePath) Then
                Dim PLT As PltFile = LoadPltForResize(CurrentFilePath)
                newWidth = PLT.GetPltWidth()
                newHeight = PLT.GetPltHeight()

                If ShowResizeDialog(Path.GetExtension(CurrentFilePath), PLT.GetPltWidth(), PLT.GetPltHeight(), newWidth, newHeight, overwriteOriginal) <> DialogResult.OK Then Return
                ResizeOverwriteOriginal = overwriteOriginal
                SaveApplicationSettings()

                If newWidth = PLT.GetPltWidth() AndAlso newHeight = PLT.GetPltHeight() Then
                    MessageBox.Show("The requested resolution is the same as the current file.", ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If

                If overwriteOriginal Then
                    SaveResizedImageOverOriginal(CurrentFilePath, Sub(tempPath) PLT.SaveResized(tempPath, newWidth, newHeight))
                    LoadImage(CurrentFilePath)
                    Return
                End If

                Dim outputPath As String = PromptForResizedImageOutputPath(CurrentFilePath, newWidth, newHeight)
                If String.IsNullOrWhiteSpace(outputPath) Then Return
                PLT.SaveResized(outputPath, newWidth, newHeight)
                LoadImage(outputPath)
                Return
            End If

            Dim outputFormat As TextureResizeOutputFormat = GetResizeOutputFormat(CurrentFilePath)
            Using sourceBitmap As Bitmap = LoadBitmapForResize(CurrentFilePath)
                newWidth = sourceBitmap.Width
                newHeight = sourceBitmap.Height

                If ShowResizeDialog(Path.GetExtension(CurrentFilePath), sourceBitmap.Width, sourceBitmap.Height, newWidth, newHeight, overwriteOriginal) <> DialogResult.OK Then Return
                ResizeOverwriteOriginal = overwriteOriginal
                SaveApplicationSettings()

                If newWidth = sourceBitmap.Width AndAlso newHeight = sourceBitmap.Height Then
                    MessageBox.Show("The requested resolution is the same as the current file.", ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If

                If overwriteOriginal Then
                    SaveResizedImageOverOriginal(CurrentFilePath, Sub(tempPath) SaveBitmapResizeToPath(sourceBitmap, tempPath, outputFormat, newWidth, newHeight))
                    LoadImage(CurrentFilePath)
                    Return
                End If

                Dim outputPath As String = PromptForResizedImageOutputPath(CurrentFilePath, newWidth, newHeight)
                If String.IsNullOrWhiteSpace(outputPath) Then Return
                SaveBitmapResizeToPath(sourceBitmap, outputPath, outputFormat, newWidth, newHeight)
                LoadImage(outputPath)
            End Using
        Catch ex As Exception
            MessageBox.Show("Could not resize image:" & Environment.NewLine & ex.Message, ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function LoadPltForResize(FilePath As String) As PltFile
        Using fileStream As New FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Return New PltFile(fileStream)
        End Using
    End Function

    Private Function LoadBitmapForResize(FilePath As String) As Bitmap
        If IsBlpFile(FilePath) Then
            Using fileStream As New FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Using BLP = New BlpFile(fileStream)
                    If BLP.GetIsValidVersion = False Then Throw New InvalidDataException("Unsupported BLP file.")
                    Dim mipmapIndex As Integer = CurrentMipMapIndex
                    If mipmapIndex < 0 OrElse mipmapIndex >= BLP.MipMapCount OrElse Not BLP.IsMipmapReadable(mipmapIndex) Then mipmapIndex = BLP.GetFirstReadableMipmapIndex()
                    If mipmapIndex < 0 Then Throw New InvalidDataException("This BLP file does not contain readable image data.")
                    Return BLP.GetBitmap(mipmapIndex)
                End Using
            End Using
        End If

        If IsDdsFile(FilePath) Then
            Using fileStream As New FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Dim DDS As New DdsFile(fileStream)
                Return DDS.GetBitmap(CurrentMipMapIndex)
            End Using
        End If

        If IsTgaFile(FilePath) Then
            Using fileStream As New FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Dim TGA As New TgaFile(fileStream)
                Return TGA.GetBitmap()
            End Using
        End If

        If IsIcoFile(FilePath) Then
            Using fileStream As New FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Dim ICO As New IcoFile(fileStream)
                Return ICO.GetBitmap(CurrentMipMapIndex)
            End Using
        End If

        Using image As Image = Image.FromFile(FilePath)
            Return CloneAs32BppArgb(image)
        End Using
    End Function

    Private Function PromptForResizedImageOutputPath(SourcePath As String, NewWidth As Integer, NewHeight As Integer) As String
        Using saveDialog As New SaveFileDialog()
            Dim extension As String = Path.GetExtension(SourcePath)
            saveDialog.Title = "Save Resized Image As"
            saveDialog.Filter = GetResizeSaveDialogFilter(extension)
            saveDialog.AddExtension = True
            saveDialog.DefaultExt = extension.TrimStart("."c)
            saveDialog.OverwritePrompt = True
            saveDialog.FileName = Path.GetFileNameWithoutExtension(SourcePath) & "_" & NewWidth.ToString() & "x" & NewHeight.ToString() & extension.ToLowerInvariant()
            saveDialog.InitialDirectory = Path.GetDirectoryName(SourcePath)

            If saveDialog.ShowDialog(Me) <> DialogResult.OK Then Return String.Empty

            If Path.GetFullPath(saveDialog.FileName).Equals(Path.GetFullPath(SourcePath), StringComparison.OrdinalIgnoreCase) Then
                MessageBox.Show("Choose a different output file, or enable overwrite original in the resize dialog.", ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return String.Empty
            End If

            Return saveDialog.FileName
        End Using
    End Function

    Private Sub SaveBitmapResizeToPath(SourceBitmap As Bitmap, OutputPath As String, OutputFormat As TextureResizeOutputFormat, NewWidth As Integer, NewHeight As Integer)
        TextureResizeWriters.SaveResizedBitmap(SourceBitmap, OutputPath, OutputFormat, NewWidth, NewHeight, TransparencyBackgroundColor)
    End Sub

    Private Function GetResizeOutputFormat(FilePathOrExtension As String) As TextureResizeOutputFormat
        Dim extension As String = FilePathOrExtension
        If Not extension.StartsWith(".", StringComparison.Ordinal) Then extension = Path.GetExtension(FilePathOrExtension)

        Select Case extension.ToUpperInvariant()
            Case ".BLP"
                Return TextureResizeOutputFormat.Blp
            Case ".DDS"
                Return TextureResizeOutputFormat.Dds
            Case ".TGA"
                Return TextureResizeOutputFormat.Tga
            Case ".ICO"
                Return TextureResizeOutputFormat.Ico
            Case ".PNG"
                Return TextureResizeOutputFormat.Png
            Case ".JPG", ".JPEG"
                Return TextureResizeOutputFormat.Jpeg
        End Select

        Throw New InvalidDataException("Unsupported resize output format: " & extension)
    End Function

    Private Function GetResizeSaveDialogFilter(FileExtension As String) As String
        Select Case FileExtension.ToUpperInvariant()
            Case ".BLP"
                Return "Blizzard Picture (*.blp)|*.blp"
            Case ".DDS"
                Return "DirectDraw Surface (*.dds)|*.dds"
            Case ".PLT"
                Return "Neverwinter Nights PLT (*.plt)|*.plt"
            Case ".TGA"
                Return "Truevision TGA (*.tga)|*.tga"
            Case ".ICO"
                Return "Windows Icon (*.ico)|*.ico"
            Case ".PNG"
                Return "PNG Image (*.png)|*.png"
            Case ".JPG", ".JPEG"
                Return "JPEG Image (*.jpg;*.jpeg)|*.jpg;*.jpeg"
        End Select

        Return "Image Files (*" & FileExtension.ToLowerInvariant() & ")|*" & FileExtension.ToLowerInvariant()
    End Function

    Private Sub SaveResizedImageOverOriginal(OriginalPath As String, SaveTemporaryFile As Action(Of String))
        If SaveTemporaryFile Is Nothing Then Throw New ArgumentNullException("SaveTemporaryFile")

        Dim directoryPath As String = Path.GetDirectoryName(OriginalPath)
        Dim tempPath As String = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(OriginalPath) & "." & Guid.NewGuid().ToString("N") & ".resize.tmp")

        Try
            SaveTemporaryFile(tempPath)
            File.Replace(tempPath, OriginalPath, Nothing)
        Finally
            If File.Exists(tempPath) Then
                Try
                    File.Delete(tempPath)
                Catch
                End Try
            End If
        End Try
    End Sub

    Private Function ShowResizeDialog(FileExtension As String, CurrentWidth As Integer, CurrentHeight As Integer, ByRef NewWidth As Integer, ByRef NewHeight As Integer, ByRef OverwriteOriginal As Boolean) As DialogResult
        Dim selectedWidth As Integer = CurrentWidth
        Dim selectedHeight As Integer = CurrentHeight
        Dim selectedOverwriteOriginal As Boolean = OverwriteOriginal

        Using dialog As New Form()
            dialog.Text = "Resize " & FileExtension.TrimStart("."c).ToUpperInvariant() & " Resolution"
            dialog.StartPosition = FormStartPosition.CenterParent
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog
            dialog.MinimizeBox = False
            dialog.MaximizeBox = False
            dialog.ShowInTaskbar = False
            dialog.ClientSize = New Size(320, 188)
            If Icon IsNot Nothing Then dialog.Icon = Icon

            Dim layout As New TableLayoutPanel()
            layout.Dock = DockStyle.Fill
            layout.Padding = New Padding(12)
            layout.ColumnCount = 2
            layout.RowCount = 5
            layout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 92.0F))
            layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

            Dim currentLabel As New Label()
            currentLabel.AutoSize = True
            currentLabel.Dock = DockStyle.Fill
            currentLabel.Text = "Current: " & CurrentWidth.ToString() & " x " & CurrentHeight.ToString()
            layout.SetColumnSpan(currentLabel, 2)

            Dim widthLabel As New Label()
            widthLabel.AutoSize = True
            widthLabel.Dock = DockStyle.Fill
            widthLabel.Text = "Width"
            widthLabel.TextAlign = ContentAlignment.MiddleLeft

            Dim widthInput As New NumericUpDown()
            widthInput.Dock = DockStyle.Fill
            widthInput.Minimum = 1D
            widthInput.Maximum = MaxTextureResizeDimension
            widthInput.Value = Math.Min(MaxTextureResizeDimension, Math.Max(1, CurrentWidth))

            Dim heightLabel As New Label()
            heightLabel.AutoSize = True
            heightLabel.Dock = DockStyle.Fill
            heightLabel.Text = "Height"
            heightLabel.TextAlign = ContentAlignment.MiddleLeft

            Dim heightInput As New NumericUpDown()
            heightInput.Dock = DockStyle.Fill
            heightInput.Minimum = 1D
            heightInput.Maximum = MaxTextureResizeDimension
            heightInput.Value = Math.Min(MaxTextureResizeDimension, Math.Max(1, CurrentHeight))

            Dim overwriteCheckBox As New CheckBox()
            overwriteCheckBox.AutoSize = True
            overwriteCheckBox.Checked = selectedOverwriteOriginal
            overwriteCheckBox.Margin = New Padding(0, 8, 0, 0)
            overwriteCheckBox.Text = "Overwrite original file"
            layout.SetColumnSpan(overwriteCheckBox, 2)

            Dim buttonPanel As New FlowLayoutPanel()
            buttonPanel.Dock = DockStyle.Fill
            buttonPanel.FlowDirection = FlowDirection.RightToLeft
            buttonPanel.WrapContents = False
            buttonPanel.Padding = New Padding(0, 10, 0, 0)
            layout.SetColumnSpan(buttonPanel, 2)

            Dim okButton As New Button()
            okButton.Text = "Save"
            okButton.DialogResult = DialogResult.OK
            okButton.Size = New Size(82, 26)

            Dim cancelButton As New Button()
            cancelButton.Text = "Cancel"
            cancelButton.DialogResult = DialogResult.Cancel
            cancelButton.Size = New Size(82, 26)

            AddHandler okButton.Click,
                Sub()
                    Dim requestedWidth As Integer = CInt(widthInput.Value)
                    Dim requestedHeight As Integer = CInt(heightInput.Value)

                    If Not ValidateResizeDimensionsForDialog(FileExtension, requestedWidth, requestedHeight) Then
                        dialog.DialogResult = DialogResult.None
                        Return
                    End If

                    selectedWidth = requestedWidth
                    selectedHeight = requestedHeight
                    selectedOverwriteOriginal = overwriteCheckBox.Checked
                End Sub

            buttonPanel.Controls.Add(okButton)
            buttonPanel.Controls.Add(cancelButton)

            layout.Controls.Add(currentLabel, 0, 0)
            layout.Controls.Add(widthLabel, 0, 1)
            layout.Controls.Add(widthInput, 1, 1)
            layout.Controls.Add(heightLabel, 0, 2)
            layout.Controls.Add(heightInput, 1, 2)
            layout.Controls.Add(overwriteCheckBox, 0, 3)
            layout.Controls.Add(buttonPanel, 0, 4)

            dialog.Controls.Add(layout)
            dialog.AcceptButton = okButton
            dialog.CancelButton = cancelButton

            Dim result As DialogResult = dialog.ShowDialog(Me)
            If result = DialogResult.OK Then
                NewWidth = selectedWidth
                NewHeight = selectedHeight
                OverwriteOriginal = selectedOverwriteOriginal
            End If

            Return result
        End Using
    End Function

    Private Function ValidateResizeDimensionsForDialog(FileExtension As String, RequestedWidth As Integer, RequestedHeight As Integer) As Boolean
        Try
            If RequestedWidth > MaxTextureResizeDimension OrElse RequestedHeight > MaxTextureResizeDimension Then
                Throw New InvalidDataException("Texture dimensions cannot be larger than " & MaxTextureResizeDimension.ToString() & " pixels.")
            End If

            If FileExtension.Equals(".plt", StringComparison.OrdinalIgnoreCase) Then
                Dim pixelBytes As Long = CLng(RequestedWidth) * CLng(RequestedHeight) * 2L
                If RequestedWidth <= 0 OrElse RequestedHeight <= 0 OrElse pixelBytes <= 0L OrElse pixelBytes > Integer.MaxValue Then Throw New InvalidDataException("The requested PLT resolution is too large to save safely.")
            Else
                TextureResizeWriters.ValidateResizeDimensions(GetResizeOutputFormat(FileExtension), RequestedWidth, RequestedHeight)
            End If

            Return True
        Catch ex As Exception
            MessageBox.Show(ex.Message, ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End Try
    End Function

    Private Sub TxtbxWoWPath_TextChanged(sender As Object, e As EventArgs) Handles TxBxTextureDirectory.TextChanged
        If TxBxTextureDirectory.Text.Length > 1 AndAlso Not TxBxTextureDirectory.Text.EndsWith("\") Then
            TxBxTextureDirectory.Text = EnsureTrailingSlash(TxBxTextureDirectory.Text)
            TxBxTextureDirectory.SelectionStart = TxBxTextureDirectory.Text.Length
        End If
    End Sub

    Private Sub OpenImage_OFD1_Click(sender As Object, e As EventArgs) 
        OFD1TextureToSplit.Filter = "All Supported Image Files|*.BLP;*.DDS;*.PLT;*.TGA;*.ICO;*.PNG;*.JPG;*.JPEG|" &
                                    "Blizzard Picture (*.BLP)|*.BLP|" &
                                    "DirectDraw Surface (*.DDS)|*.DDS|" &
                                    "Neverwinter Nights PLT (*.PLT)|*.PLT|" &
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
            CurrentPltSelectedLayer = -1
            CurrentPltLayerIds = New Integer() {}

            TxBxTextureName.Text = Path.GetFileName(CurrentFilePath)
            TxBxTextureDirectory.Text = EnsureTrailingSlash(Path.GetDirectoryName(CurrentFilePath))
            ResetFileInfo()
            ResetPreviewZoomForNewImage()

            If IsBlpFile(CurrentFilePath) Then
                LoadBlpImage(CurrentFilePath)
            ElseIf IsDdsFile(CurrentFilePath) Then
                LoadDdsImage(CurrentFilePath)
            ElseIf IsPltFile(CurrentFilePath) Then
                LoadPltImage(CurrentFilePath)
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
                If BLP.GetReadableMipMapCount() <= 0 Then Throw New InvalidDataException("This BLP file does not contain readable mipmap data.")

                PopulateBlpInfo(BLP)
                CurrentMipMapIndex = BLP.GetFirstReadableMipmapIndex()
                If CurrentMipMapIndex < 0 Then Throw New InvalidDataException("This BLP file does not contain readable mipmap data.")
                SetSourceBitmap(BLP.GetBitmap(CurrentMipMapIndex))
                SetActiveMipmapInfo(CurrentMipMapIndex, BLP.GetMipmapWidth(CurrentMipMapIndex), BLP.GetMipmapHeight(CurrentMipMapIndex))

                If LsBxMipMapList.Items.Count > CurrentMipMapIndex + 1 Then LsBxMipMapList.SelectedIndex = CurrentMipMapIndex + 1
            End Using
        End Using
    End Sub

    Private Sub LoadDdsImage(FilePath As String)
        Using fileStream As New FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Dim DDS As New DdsFile(fileStream)
            If DDS.MipMapCount <= 0 Then Throw New InvalidDataException("This DDS file does not contain readable mipmap data.")

            PopulateDdsInfo(DDS)
            CurrentMipMapIndex = 0
            SetSourceBitmap(GetDdsBitmapForDisplay(DDS, CurrentMipMapIndex))
            SetActiveMipmapInfo(CurrentMipMapIndex, DDS.GetMipmapWidth(CurrentMipMapIndex), DDS.GetMipmapHeight(CurrentMipMapIndex))

            If LsBxMipMapList.Items.Count > 1 Then LsBxMipMapList.SelectedIndex = 1
        End Using
    End Sub

    Private Function GetDdsBitmapForDisplay(DDS As DdsFile, MipmapIndex As Integer) As Bitmap
        Dim bitmap As Bitmap = DDS.GetBitmap(MipmapIndex)
        If DDS.GetIsBioWareCompact() Then
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY)
        End If

        Return bitmap
    End Function

    Private Sub LoadPltImage(FilePath As String)
        Using fileStream As New FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Dim PLT As New PltFile(fileStream)

            PopulatePltInfo(PLT)
            CurrentMipMapIndex = 0
            SetSourceBitmap(PLT.GetBitmap())
            SetActiveMipmapInfo(0, PLT.GetPltWidth(), PLT.GetPltHeight())

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
        LsBxMipMapList.Items.Add(" # | Size      | Bytes      | Offset   | Status")

        For i As Integer = 0 To BLP.MipMapCount - 1
            LsBxMipMapList.Items.Add(String.Format("{0,2} | {1,4}x{2,-4} | {3,10} | {4,8} | {5}",
                                                    i,
                                                    BLP.GetMipmapWidth(i),
                                                    BLP.GetMipmapHeight(i),
                                                    BLP.GetBLPMipMapSize(i),
                                                    BLP.GetBLPMipMapOffset(i),
                                                    BLP.GetMipmapStatusText(i)))
        Next

        Dim readableMipMapCount As Integer = BLP.GetReadableMipMapCount()
        If readableMipMapCount = BLP.MipMapCount Then
            LblMipMapCountValue.Text = BLP.MipMapCount.ToString()
        Else
            LblMipMapCountValue.Text = BLP.MipMapCount.ToString() & " (" & readableMipMapCount.ToString() & " readable)"
        End If

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

    Private Sub PopulatePltInfo(PLT As PltFile)
        LsBxMipMapList.Items.Clear()
        LsBxMipMapList.Items.Add(" # | Size      | Bytes    | Offset")
        LsBxMipMapList.Items.Add(String.Format("{0,2} | {1,4}x{2,-4} | {3,8} | {4}",
                                                0,
                                                PLT.GetPltWidth(),
                                                PLT.GetPltHeight(),
                                                PLT.GetPixelDataLength(),
                                                PLT.GetPixelDataOffset()))
        LsBxMipMapList.Items.Add("Layer | Name      | Pixels")

        CurrentPltLayerIds = PLT.GetPresentLayerIds()
        For Each layerId As Integer In CurrentPltLayerIds
            LsBxMipMapList.Items.Add(String.Format("{0,5} | {1,-9} | {2}",
                                                    layerId,
                                                    PltFile.GetLayerName(layerId),
                                                    PLT.GetLayerPixelCount(layerId)))
        Next

        LblCompressionValue.Text = "PLT " & PLT.GetVersionText()
        LblAlphaEncodingValue.Text = PLT.GetFormatName() & " - " & PLT.GetPreviewModeText()
        LblAlphaChannelValue.Text = PLT.GetAlphaChannelText()
        LblMipMapCountValue.Text = "1 (" & CurrentPltLayerIds.Length.ToString() & " layers)"
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
                    If BLP.GetReadableMipMapCount() <= 0 Then Return

                    If MipmapIndex < 0 Then MipmapIndex = 0
                    If MipmapIndex >= BLP.MipMapCount Then MipmapIndex = BLP.MipMapCount - 1
                    If Not BLP.IsMipmapReadable(MipmapIndex) Then Return

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
                SetSourceBitmap(GetDdsBitmapForDisplay(DDS, CurrentMipMapIndex))
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

    Private Sub LoadCurrentPltLayerFromListIndex(ListIndex As Integer)
        If String.IsNullOrWhiteSpace(CurrentFilePath) OrElse Not IsPltFile(CurrentFilePath) Then Return

        Dim layerId As Integer = -1
        If ListIndex >= 3 Then
            Dim layerIndex As Integer = ListIndex - 3
            If layerIndex < 0 OrElse layerIndex >= CurrentPltLayerIds.Length Then Return
            layerId = CurrentPltLayerIds(layerIndex)
        ElseIf ListIndex <> 1 Then
            Return
        End If

        Try
            Using fileStream As New FileStream(CurrentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Dim PLT As New PltFile(fileStream)

                CurrentPltSelectedLayer = layerId
                CurrentMipMapIndex = 0
                SetSourceBitmap(PLT.GetBitmap(CurrentPltSelectedLayer))
                SetActiveMipmapInfo(0, PLT.GetPltWidth(), PLT.GetPltHeight())
            End Using
        Catch ex As Exception
            MessageBox.Show("Could not load PLT layer preview:" & Environment.NewLine & ex.Message, "BLP Orrery", MessageBoxButtons.OK, MessageBoxIcon.Error)
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
        EndPreviewPanning()
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

    Private Sub InitializeShellExtensionMenu()
        RefreshShellExtensionMenu(False)
    End Sub

    Private Sub ExplorerThumbnailsToolStripMenuItem_DropDownOpening(sender As Object, e As EventArgs) Handles ExplorerThumbnailsToolStripMenuItem.DropDownOpening
        RefreshShellExtensionMenu(False)
    End Sub

    Private Sub RefreshShellExtensionMenu(PreserveSelections As Boolean)
        Try
            Dim status As OrreryShellStatus = OrreryShellExtensionManager.GetStatus()
            ShellExtensionStatus = status
            IsUpdatingShellExtensionMenu = True

            If Not PreserveSelections Then
                ShellEnableMenuItem.Checked = status.IsActive

                Dim selectedFormats As HashSet(Of String)
                If status.IsActive Then
                    selectedFormats = New HashSet(Of String)(status.ActiveFormats, StringComparer.OrdinalIgnoreCase)
                Else
                    selectedFormats = OrreryShellExtensionManager.GetPreferredFormats()
                End If

                ShellBlpMenuItem.Checked = selectedFormats.Contains(".blp")
                ShellDdsMenuItem.Checked = selectedFormats.Contains(".dds")
                ShellPltMenuItem.Checked = selectedFormats.Contains(".plt")
                ShellIcoMenuItem.Checked = selectedFormats.Contains(".ico")
            End If

            ShellStatusMenuItem.Text = status.GetMenuSummary()
            ShellApplyMenuItem.Enabled = status.PayloadAvailable
            ShellEnableMenuItem.Enabled = status.PayloadAvailable OrElse status.IsActive
            ShellFormatsMenuItem.Enabled = status.PayloadAvailable OrElse status.IsActive
            ShellOpenFolderMenuItem.Enabled = Directory.Exists(OrreryShellExtensionManager.GetInstallRoot())
        Catch ex As Exception
            ShellExtensionStatus = Nothing
            ShellStatusMenuItem.Text = "Status: unable to inspect registration"
            ShellApplyMenuItem.Enabled = False
            ShellEnableMenuItem.Enabled = False
            ShellFormatsMenuItem.Enabled = False
        Finally
            IsUpdatingShellExtensionMenu = False
        End Try
    End Sub

    Private Function GetSelectedShellExtensions() As List(Of String)
        Dim selected As New List(Of String)()
        If ShellBlpMenuItem.Checked Then selected.Add(".blp")
        If ShellDdsMenuItem.Checked Then selected.Add(".dds")
        If ShellPltMenuItem.Checked Then selected.Add(".plt")
        If ShellIcoMenuItem.Checked Then selected.Add(".ico")
        Return selected
    End Function

    Private Sub ApplyShellExtensionMenuSelection(ShowConfirmation As Boolean)
        If IsUpdatingShellExtensionMenu Then Return

        Dim previousCursor As Cursor = Cursor
        Try
            Cursor = Cursors.WaitCursor
            ExplorerThumbnailsToolStripMenuItem.Enabled = False

            Dim selected As List(Of String) = GetSelectedShellExtensions()
            Dim enableProvider As Boolean = ShellEnableMenuItem.Checked AndAlso selected.Count > 0
            ShellExtensionStatus = OrreryShellExtensionManager.Apply(enableProvider, selected)
            RefreshShellExtensionMenu(False)

            If ShowConfirmation Then
                MessageBox.Show(ShellExtensionStatus.GetDetails(), ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            MessageBox.Show("Could not apply the Explorer thumbnail settings." & Environment.NewLine & Environment.NewLine & ex.Message,
                            ApplicationTitle,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
            RefreshShellExtensionMenu(False)
        Finally
            ExplorerThumbnailsToolStripMenuItem.Enabled = True
            Cursor = previousCursor
        End Try
    End Sub

    Private Sub ShellEnableMenuItem_Click(sender As Object, e As EventArgs) Handles ShellEnableMenuItem.Click
        ApplyShellExtensionMenuSelection(False)
    End Sub

    Private Sub ShellFormatMenuItem_Click(sender As Object, e As EventArgs) Handles ShellBlpMenuItem.Click, ShellDdsMenuItem.Click, ShellPltMenuItem.Click, ShellIcoMenuItem.Click
        ApplyShellExtensionMenuSelection(False)
    End Sub

    Private Sub ShellApplyMenuItem_Click(sender As Object, e As EventArgs) Handles ShellApplyMenuItem.Click
        ApplyShellExtensionMenuSelection(True)
    End Sub

    Private Sub ShellRefreshMenuItem_Click(sender As Object, e As EventArgs) Handles ShellRefreshMenuItem.Click
        Try
            OrreryShellExtensionManager.NotifyShellAssociationsChanged()
            RefreshShellExtensionMenu(False)
        Catch ex As Exception
            MessageBox.Show("Windows Explorer could not be refreshed." & Environment.NewLine & Environment.NewLine & ex.Message,
                            ApplicationTitle,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ShellDetailsMenuItem_Click(sender As Object, e As EventArgs) Handles ShellDetailsMenuItem.Click
        RefreshShellExtensionMenu(False)
        Dim details As String = If(ShellExtensionStatus Is Nothing,
                                   "The Explorer thumbnail provider status could not be read.",
                                   ShellExtensionStatus.GetDetails())
        MessageBox.Show(details, ApplicationTitle & " Explorer Thumbnails", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub ShellOpenFolderMenuItem_Click(sender As Object, e As EventArgs) Handles ShellOpenFolderMenuItem.Click
        Try
            Dim installRoot As String = OrreryShellExtensionManager.GetInstallRoot()
            If Not Directory.Exists(installRoot) Then Directory.CreateDirectory(installRoot)
            Process.Start("explorer.exe", installRoot)
        Catch ex As Exception
            MessageBox.Show("The installed provider folder could not be opened." & Environment.NewLine & Environment.NewLine & ex.Message,
                            ApplicationTitle,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        End Try
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
        ElseIf PreviewActualSizeMenuItem Is Nothing OrElse Not PreviewActualSizeMenuItem.Checked Then
            PreviewZoomIsManual = False
            PreviewZoomFactor = 1.0F
        End If

        UpdateTextureViewSizing()
    End Sub

    Private Sub PreviewActualSizeMenuItem_CheckedChanged(sender As Object, e As EventArgs) Handles PreviewActualSizeMenuItem.CheckedChanged
        If PreviewActualSizeMenuItem.Checked Then
            PreviewZoomIsManual = True
            PreviewZoomFactor = 1.0F
        ElseIf ResizeByTextureMenuItem Is Nothing OrElse Not ResizeByTextureMenuItem.Checked Then
            PreviewZoomIsManual = False
            PreviewZoomFactor = 1.0F
        End If

        If Not IsLoadingImage Then ApplyPreviewZoomLayout()
    End Sub

    Private Sub FlipBioWareDdsMenuItem_CheckedChanged(sender As Object, e As EventArgs) Handles FlipBioWareDdsMenuItem.CheckedChanged
        If Not IsLoadingImage AndAlso IsDdsFile(CurrentFilePath) Then LoadCurrentDdsMipmap(CurrentMipMapIndex)
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
            ElseIf IsPltFile(CurrentFilePath) Then
                LoadCurrentPltLayerFromListIndex(LsBxMipMapList.SelectedIndex)
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

    Private Sub PicBxTextureView_MouseDown(sender As Object, e As MouseEventArgs) Handles PicBxTextureView.MouseDown
        If e.Button <> MouseButtons.Left OrElse Not CanPanPreview() Then Return

        IsPreviewPanning = True
        PreviewPanStartMousePosition = PicBxTextureView.PointToScreen(e.Location)
        PreviewPanStartScrollPosition = New Point(GetHorizontalScrollOffset(), GetVerticalScrollOffset())
        PicBxTextureView.Capture = True
        PicBxTextureView.Cursor = Cursors.SizeAll
    End Sub

    Private Sub PicBxTextureView_MouseMove(sender As Object, e As MouseEventArgs) Handles PicBxTextureView.MouseMove
        If Not IsPreviewPanning Then
            UpdatePreviewPanCursor()
            Return
        End If

        If (Control.MouseButtons And MouseButtons.Left) <> MouseButtons.Left Then
            EndPreviewPanning()
            Return
        End If

        Dim currentMousePosition As Point = PicBxTextureView.PointToScreen(e.Location)
        Dim desiredX As Integer = PreviewPanStartScrollPosition.X - (currentMousePosition.X - PreviewPanStartMousePosition.X)
        Dim desiredY As Integer = PreviewPanStartScrollPosition.Y - (currentMousePosition.Y - PreviewPanStartMousePosition.Y)

        ScrollPreviewTo(desiredX, desiredY)
    End Sub

    Private Sub PicBxTextureView_MouseUp(sender As Object, e As MouseEventArgs) Handles PicBxTextureView.MouseUp
        If e.Button = MouseButtons.Left Then EndPreviewPanning()
    End Sub

    Private Sub PicBxTextureView_MouseLeave(sender As Object, e As EventArgs) Handles PicBxTextureView.MouseLeave
        If Not IsPreviewPanning Then PicBxTextureView.Cursor = Cursors.Default
    End Sub

    Private Sub PicBxTextureView_MouseCaptureChanged(sender As Object, e As EventArgs) Handles PicBxTextureView.MouseCaptureChanged
        If IsPreviewPanning AndAlso Not PicBxTextureView.Capture Then EndPreviewPanning()
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
        If BtnPreviousImage Is Nothing OrElse BtnNextImage Is Nothing OrElse BtnZoomOut Is Nothing OrElse BtnZoomIn Is Nothing OrElse BtnResizePlt Is Nothing OrElse PnlToolBar Is Nothing Then Return

        Dim buttonGap As Integer = 6
        Dim buttonLeft As Integer = 4
        Dim buttonTop As Integer = Math.Max(4, PnlToolBar.ClientSize.Height - BtnPreviousImage.Height - 4)

        BtnPreviousImage.Location = New Point(buttonLeft, buttonTop)
        BtnNextImage.Location = New Point(buttonLeft + BtnPreviousImage.Width + buttonGap, buttonTop)
        BtnZoomOut.Location = New Point(BtnNextImage.Right + buttonGap, buttonTop)
        BtnZoomIn.Location = New Point(BtnZoomOut.Right + buttonGap, buttonTop)
        BtnResizePlt.Location = New Point(BtnZoomIn.Right + buttonGap, buttonTop)
        BtnPreviousImage.BringToFront()
        BtnNextImage.BringToFront()
        BtnZoomOut.BringToFront()
        BtnZoomIn.BringToFront()
        BtnResizePlt.BringToFront()
    End Sub

    Private Sub UpdateNavigationButtons()
        If BtnPreviousImage Is Nothing OrElse BtnNextImage Is Nothing Then Return

        Dim hasSiblingImages As Boolean = CanNavigateSiblingImages()
        BtnPreviousImage.Enabled = hasSiblingImages
        BtnNextImage.Enabled = hasSiblingImages
    End Sub

    Private Sub ResetPreviewZoomForNewImage()
        PreviewZoomFactor = 1.0F
        PreviewZoomIsManual = ShouldPreviewAtActualSize()
    End Sub

    Private Sub UpdateTextureViewSizing(Optional TextureWidth As Integer = 0, Optional TextureHeight As Integer = 0)
        If PicBxTextureView Is Nothing OrElse ResizeByTextureMenuItem Is Nothing Then Return

        If ShouldPreviewAtActualSize() Then
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
        Dim nonPreviewWidth As Integer = Math.Max(0, ClientSize.Width - PnlTextureView.ClientSize.Width)
        Dim nonPreviewHeight As Integer = Math.Max(0, ClientSize.Height - PnlTextureView.ClientSize.Height)
        Dim minClientWidth As Integer = Math.Max(1, MinimumSize.Width - Math.Max(0, Size.Width - ClientSize.Width))
        Dim minClientHeight As Integer = Math.Max(1, MinimumSize.Height - Math.Max(0, Size.Height - ClientSize.Height))
        Dim targetClientWidth As Integer = Math.Max(minClientWidth, nonPreviewWidth + targetPreviewWidth)
        Dim targetClientHeight As Integer = Math.Max(minClientHeight, nonPreviewHeight + targetPreviewHeight)

        If ClientSize.Width = targetClientWidth AndAlso ClientSize.Height = targetClientHeight Then Return

        ClientSize = New Size(targetClientWidth, targetClientHeight)
        PerformLayout()
        PositionNavigationButtons()
    End Sub

    Private Function ShouldPreviewAtActualSize() As Boolean
        If ResizeByTextureMenuItem IsNot Nothing AndAlso ResizeByTextureMenuItem.Checked Then Return True
        If PreviewActualSizeMenuItem IsNot Nothing AndAlso PreviewActualSizeMenuItem.Checked Then Return True
        Return False
    End Function

    Private Sub UpdateDisplayButtons()
        If BtnMask IsNot Nothing Then BtnMask.BackColor = If(MaskModeEnabled, Color.LightSteelBlue, Color.Silver)
        If BtnTransparency IsNot Nothing Then BtnTransparency.BackColor = If(RenderTransparencyMenuItem.Checked, Color.LightSteelBlue, Color.Silver)

        Dim hasImage As Boolean = CurrentSourceBitmap IsNot Nothing
        Dim canResizeImage As Boolean = hasImage AndAlso Not String.IsNullOrWhiteSpace(CurrentFilePath) AndAlso IsSupportedImageFile(CurrentFilePath)
        If BtnZoomOut IsNot Nothing Then BtnZoomOut.Enabled = hasImage
        If BtnZoomIn IsNot Nothing Then BtnZoomIn.Enabled = hasImage
        If BtnResizePlt IsNot Nothing Then BtnResizePlt.Enabled = canResizeImage
        If ResizePltToolStripMenuItem IsNot Nothing Then ResizePltToolStripMenuItem.Enabled = canResizeImage
    End Sub

    Private Sub ZoomPreview(ZoomIn As Boolean, AnchorPoint As Point)
        If PicBxTextureView.Image Is Nothing Then Return

        Dim displayedImageBounds As RectangleF = GetDisplayedPreviewImageBounds()
        If displayedImageBounds.Width <= 0.0F OrElse displayedImageBounds.Height <= 0.0F Then Return

        Dim oldZoomFactor As Single = displayedImageBounds.Width / PicBxTextureView.Image.Width
        Dim imageX As Single = (AnchorPoint.X - displayedImageBounds.Left) / displayedImageBounds.Width * PicBxTextureView.Image.Width
        Dim imageY As Single = (AnchorPoint.Y - displayedImageBounds.Top) / displayedImageBounds.Height * PicBxTextureView.Image.Height
        imageX = Math.Max(0.0F, Math.Min(CSng(PicBxTextureView.Image.Width), imageX))
        imageY = Math.Max(0.0F, Math.Min(CSng(PicBxTextureView.Image.Height), imageY))

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

    Private Function GetDisplayedPreviewImageBounds() As RectangleF
        If PicBxTextureView.Image Is Nothing Then Return RectangleF.Empty

        If PreviewZoomIsManual Then
            Return New RectangleF(PicBxTextureView.Left, PicBxTextureView.Top, PicBxTextureView.Width, PicBxTextureView.Height)
        End If

        Dim widthScale As Single = CSng(PicBxTextureView.ClientSize.Width / CDbl(PicBxTextureView.Image.Width))
        Dim heightScale As Single = CSng(PicBxTextureView.ClientSize.Height / CDbl(PicBxTextureView.Image.Height))
        Dim fitScale As Single = Math.Min(widthScale, heightScale)
        Dim renderedWidth As Single = PicBxTextureView.Image.Width * fitScale
        Dim renderedHeight As Single = PicBxTextureView.Image.Height * fitScale
        Dim renderedLeft As Single = PicBxTextureView.Left + (PicBxTextureView.ClientSize.Width - renderedWidth) / 2.0F
        Dim renderedTop As Single = PicBxTextureView.Top + (PicBxTextureView.ClientSize.Height - renderedHeight) / 2.0F

        Return New RectangleF(renderedLeft, renderedTop, renderedWidth, renderedHeight)
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

    Private Function CanPanPreview() As Boolean
        If PicBxTextureView Is Nothing OrElse PicBxTextureView.Image Is Nothing OrElse PnlTextureView Is Nothing Then Return False
        Return PnlTextureView.HorizontalScroll.Visible OrElse PnlTextureView.VerticalScroll.Visible
    End Function

    Private Sub ScrollPreviewTo(HorizontalOffset As Integer, VerticalOffset As Integer)
        Dim maximumX As Integer = If(PnlTextureView.HorizontalScroll.Visible,
                                     Math.Max(0, PnlTextureView.HorizontalScroll.Maximum - PnlTextureView.HorizontalScroll.LargeChange + 1),
                                     0)
        Dim maximumY As Integer = If(PnlTextureView.VerticalScroll.Visible,
                                     Math.Max(0, PnlTextureView.VerticalScroll.Maximum - PnlTextureView.VerticalScroll.LargeChange + 1),
                                     0)
        Dim targetX As Integer = Math.Max(0, Math.Min(maximumX, HorizontalOffset))
        Dim targetY As Integer = Math.Max(0, Math.Min(maximumY, VerticalOffset))

        PnlTextureView.AutoScrollPosition = New Point(targetX, targetY)
    End Sub

    Private Sub EndPreviewPanning()
        If Not IsPreviewPanning Then Return

        IsPreviewPanning = False
        If PicBxTextureView IsNot Nothing AndAlso PicBxTextureView.Capture Then PicBxTextureView.Capture = False
        UpdatePreviewPanCursor()
    End Sub

    Private Sub UpdatePreviewPanCursor()
        If PicBxTextureView Is Nothing Then Return
        PicBxTextureView.Cursor = If(CanPanPreview(), Cursors.SizeAll, Cursors.Default)
    End Sub

    Private Sub ScrollPreviewToAnchor(ImageX As Single, ImageY As Single, AnchorPoint As Point)
        If PicBxTextureView.Image Is Nothing Then Return

        Dim renderedX As Double = PicBxTextureView.Left + ImageX / PicBxTextureView.Image.Width * PicBxTextureView.Width
        Dim renderedY As Double = PicBxTextureView.Top + ImageY / PicBxTextureView.Image.Height * PicBxTextureView.Height
        Dim desiredX As Integer = GetHorizontalScrollOffset() + CInt(Math.Round(renderedX - AnchorPoint.X))
        Dim desiredY As Integer = GetVerticalScrollOffset() + CInt(Math.Round(renderedY - AnchorPoint.Y))

        ScrollPreviewTo(desiredX, desiredY)
    End Sub

    Private Sub ApplyPreviewZoomLayout()
        If PicBxTextureView Is Nothing OrElse PnlTextureView Is Nothing Then Return

        If PicBxTextureView.Image Is Nothing Then
            PnlTextureView.AutoScroll = False
            PicBxTextureView.Dock = DockStyle.Fill
            PicBxTextureView.SizeMode = PictureBoxSizeMode.Zoom
            UpdatePreviewPanCursor()
            Return
        End If

        If Not PreviewZoomIsManual Then
            PnlTextureView.AutoScroll = False
            PicBxTextureView.Dock = DockStyle.Fill
            PicBxTextureView.SizeMode = PictureBoxSizeMode.Zoom
            UpdatePreviewPanCursor()
            Return
        End If

        Dim previousScrollX As Integer = GetHorizontalScrollOffset()
        Dim previousScrollY As Integer = GetVerticalScrollOffset()

        PnlTextureView.AutoScroll = True
        PnlTextureView.AutoScrollPosition = Point.Empty
        PicBxTextureView.Dock = DockStyle.None
        PicBxTextureView.SizeMode = PictureBoxSizeMode.StretchImage

        Dim targetWidth As Integer = Math.Max(1, CInt(Math.Round(PicBxTextureView.Image.Width * CDbl(PreviewZoomFactor))))
        Dim targetHeight As Integer = Math.Max(1, CInt(Math.Round(PicBxTextureView.Image.Height * CDbl(PreviewZoomFactor))))
        Dim targetLeft As Integer = If(targetWidth < PnlTextureView.ClientSize.Width, (PnlTextureView.ClientSize.Width - targetWidth) \ 2, 0)
        Dim targetTop As Integer = If(targetHeight < PnlTextureView.ClientSize.Height, (PnlTextureView.ClientSize.Height - targetHeight) \ 2, 0)

        PicBxTextureView.Bounds = New Rectangle(targetLeft, targetTop, targetWidth, targetHeight)
        ScrollPreviewTo(previousScrollX, previousScrollY)
        UpdatePreviewPanCursor()
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
        aboutWindow.ClientSize = New Size(900, 680)
        aboutWindow.MinimumSize = New Size(720, 560)
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
        rootLayout.ColumnCount = 1
        rootLayout.RowCount = 3
        rootLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        rootLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 230.0F))
        rootLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        rootLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 44.0F))

        Dim splashBox As New PictureBox()
        splashBox.Dock = DockStyle.Fill
        splashBox.Margin = New Padding(0, 0, 0, 12)
        splashBox.BackColor = Color.FromArgb(18, 20, 24)
        splashBox.BorderStyle = BorderStyle.FixedSingle
        splashBox.SizeMode = PictureBoxSizeMode.Zoom
        splashBox.Image = splashImage
        rootLayout.Controls.Add(splashBox, 0, 0)

        Dim infoLayout As New TableLayoutPanel()
        infoLayout.Dock = DockStyle.Fill
        infoLayout.ColumnCount = 1
        infoLayout.RowCount = 6
        infoLayout.Padding = New Padding(0)
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
            "PLT - Neverwinter Nights player texture layers",
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
        changelogBox.TabStop = False
        changelogBox.Text = GetAboutChangelogText()

        infoLayout.Controls.Add(titleLabel, 0, 0)
        infoLayout.Controls.Add(authorLabel, 0, 1)
        infoLayout.Controls.Add(formatsHeader, 0, 2)
        infoLayout.Controls.Add(formatsLabel, 0, 3)
        infoLayout.Controls.Add(changelogHeader, 0, 4)
        infoLayout.Controls.Add(changelogBox, 0, 5)

        rootLayout.Controls.Add(infoLayout, 0, 1)

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

        rootLayout.Controls.Add(buttonPanel, 0, 2)
        aboutWindow.AcceptButton = okButton
        aboutWindow.CancelButton = okButton
        aboutWindow.Controls.Add(rootLayout)
        AddHandler aboutWindow.Shown,
            Sub()
                changelogBox.SelectionStart = 0
                changelogBox.SelectionLength = 0
                okButton.Focus()
            End Sub

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
            "Added thematic Orrery splash artwork and this versioned changelog to the About window.",
            "2.0 - Damaged mipmap resilience" & Environment.NewLine &
            "Added strict BLP mipmap validation in Orrery and ShellExtCore, skipped unreadable table entries, marked damaged mipmaps in the list, and kept the original preview loading whenever a readable full-size image exists.",
            "2.1 - NWN PLT previews" & Environment.NewLine &
            "Added Neverwinter Nights PLT preview support in Orrery and Explorer thumbnails using a color-coded luminance/layer render.",
            "2.2 - Preview sizing and PLT layer inspection" & Environment.NewLine &
            "Added a persistent 1:1 Preview Actual Size mode, fixed cumulative Resize By Texture window growth, and made PLT layer rows selectable with dimmed non-selected layers.",
            "2.3 - Window restore failsafe" & Environment.NewLine &
            "Added a startup guard that keeps valid remembered window positions but recenters Orrery if the saved position is outside the visible monitor layout, and prevented About changelog text from opening fully selected.",
            "2.4 - PLT resolution export" & Environment.NewLine &
            "Added a PLT-only resolution control that saves a new resized PLT file by resampling luminance/layer byte pairs without flattening the layered texture data.",
            "2.5 - PLT resize overwrite option" & Environment.NewLine &
            "Added a remembered overwrite-original checkbox to the PLT resize dialog for direct in-place resolution changes when desired.",
            "2.6 - Multi-format resolution export" & Environment.NewLine &
            "Extended the resize dialog to BLP, DDS, TGA, ICO, PNG, JPG, and JPEG with format-specific writers while keeping PLT layer-pair resizing intact.",
            "2.7 - BioWare DDS orientation" & Environment.NewLine &
            "Added a remembered Settings switch for vertically flipping BioWare/NWN compact DDS previews without changing standard DDS files.",
            "2.8 - Core BioWare DDS orientation" & Environment.NewLine &
            "Moved BioWare/NWN compact DDS vertical orientation correction into the DDS decoder core so Orrery previews, exports, and Explorer thumbnails share the corrected default.",
            "2.9 - Integrated Explorer thumbnails" & Environment.NewLine &
            "Unified Orrery and ShellExtCore decoder sources, embedded the versioned Explorer provider into Orrery, added per-user on-the-fly installation and per-format controls, preserved displaced thumbnail handlers, removed the administrator and PowerShell requirement, hardened shell decoding limits and stream reads, and added mouse-drag panning with pointer-anchored zoom."
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

    Private Function IsPltFile(FilePath As String) As Boolean
        Return Path.GetExtension(FilePath).Equals(".PLT", StringComparison.OrdinalIgnoreCase)
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
