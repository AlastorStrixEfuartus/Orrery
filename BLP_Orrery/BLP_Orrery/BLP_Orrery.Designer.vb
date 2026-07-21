<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class BLP_Orrery_MainForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(BLP_Orrery_MainForm))
        Me.TxBxTextureDirectory = New System.Windows.Forms.TextBox()
        Me.PicBxTextureView = New System.Windows.Forms.PictureBox()
        Me.BtnSave = New System.Windows.Forms.Button()
        Me.TxBxTextureName = New System.Windows.Forms.TextBox()
        Me.OFD1TextureToSplit = New System.Windows.Forms.OpenFileDialog()
        Me.BLPOrreryMainMenuStrip = New System.Windows.Forms.MenuStrip()
        Me.FileToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.OpenFileToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SaveAsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ResizePltToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator()
        Me.QuitToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ViewToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.FileInformationsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.RenderTransparencyMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.BackgroundClrMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ResizeByTextureMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.PreviewActualSizeMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.FlipBioWareDdsMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ExplorerThumbnailsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ShellStatusMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ShellEnableMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ShellFormatsMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ShellBlpMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ShellDdsMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ShellPltMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ShellIcoMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ShellMenuSeparator1 = New System.Windows.Forms.ToolStripSeparator()
        Me.ShellApplyMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ShellRefreshMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ShellDetailsMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ShellOpenFolderMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AboutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.GrBxFileInfo = New System.Windows.Forms.GroupBox()
        Me.LblActiveMipMaipValue = New System.Windows.Forms.Label()
        Me.LblActiveMipMap = New System.Windows.Forms.Label()
        Me.LblResolutionValue = New System.Windows.Forms.Label()
        Me.LblResolution = New System.Windows.Forms.Label()
        Me.LblMipMapCountValue = New System.Windows.Forms.Label()
        Me.LblAlphaChannelValue = New System.Windows.Forms.Label()
        Me.LblAlphaEncodingValue = New System.Windows.Forms.Label()
        Me.LblCompressionValue = New System.Windows.Forms.Label()
        Me.LsBxMipMapList = New System.Windows.Forms.ListBox()
        Me.LblMipMapCount = New System.Windows.Forms.Label()
        Me.LblAlphaChannel = New System.Windows.Forms.Label()
        Me.LblAlphaEncoding = New System.Windows.Forms.Label()
        Me.LblCompression = New System.Windows.Forms.Label()
        Me.PnlToolBar = New System.Windows.Forms.Panel()
        Me.BtnPreviousImage = New System.Windows.Forms.Button()
        Me.BtnNextImage = New System.Windows.Forms.Button()
        Me.BtnZoomOut = New System.Windows.Forms.Button()
        Me.BtnZoomIn = New System.Windows.Forms.Button()
        Me.BtnResizePlt = New System.Windows.Forms.Button()
        Me.BtnMask = New System.Windows.Forms.Button()
        Me.BtnTransparency = New System.Windows.Forms.Button()
        Me.ComBxSaveAsFormat = New System.Windows.Forms.ComboBox()
        Me.PnlFileInfo = New System.Windows.Forms.Panel()
        Me.PnlTextureViewAndToolBar = New System.Windows.Forms.Panel()
        Me.PnlTextureView = New System.Windows.Forms.Panel()
        Me.ClrPickBackGround = New System.Windows.Forms.ColorDialog()
        CType(Me.PicBxTextureView, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.BLPOrreryMainMenuStrip.SuspendLayout()
        Me.GrBxFileInfo.SuspendLayout()
        Me.PnlToolBar.SuspendLayout()
        Me.PnlFileInfo.SuspendLayout()
        Me.PnlTextureViewAndToolBar.SuspendLayout()
        Me.PnlTextureView.SuspendLayout()
        Me.SuspendLayout()
        '
        'TxBxTextureDirectory
        '
        Me.TxBxTextureDirectory.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TxBxTextureDirectory.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.TxBxTextureDirectory.Location = New System.Drawing.Point(3, 3)
        Me.TxBxTextureDirectory.Name = "TxBxTextureDirectory"
        Me.TxBxTextureDirectory.Size = New System.Drawing.Size(494, 20)
        Me.TxBxTextureDirectory.TabIndex = 0
        Me.TxBxTextureDirectory.Tag = "14"
        Me.TxBxTextureDirectory.Text = "Put Path Here"
        '
        'PicBxTextureView
        '
        Me.PicBxTextureView.BackgroundImage = CType(resources.GetObject("PicBxTextureView.BackgroundImage"), System.Drawing.Image)
        Me.PicBxTextureView.Dock = System.Windows.Forms.DockStyle.Fill
        Me.PicBxTextureView.Location = New System.Drawing.Point(0, 0)
        Me.PicBxTextureView.Name = "PicBxTextureView"
        Me.PicBxTextureView.Size = New System.Drawing.Size(500, 441)
        Me.PicBxTextureView.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.PicBxTextureView.TabIndex = 1
        Me.PicBxTextureView.TabStop = False
        '
        'BtnSave
        '
        Me.BtnSave.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.BtnSave.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.BtnSave.Location = New System.Drawing.Point(428, 27)
        Me.BtnSave.Name = "BtnSave"
        Me.BtnSave.Size = New System.Drawing.Size(69, 25)
        Me.BtnSave.TabIndex = 2
        Me.BtnSave.Tag = "13"
        Me.BtnSave.Text = "Save As"
        Me.BtnSave.UseVisualStyleBackColor = True
        '
        'TxBxTextureName
        '
        Me.TxBxTextureName.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TxBxTextureName.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.TxBxTextureName.Location = New System.Drawing.Point(4, 26)
        Me.TxBxTextureName.Name = "TxBxTextureName"
        Me.TxBxTextureName.ReadOnly = True
        Me.TxBxTextureName.Size = New System.Drawing.Size(306, 20)
        Me.TxBxTextureName.TabIndex = 1
        '
        'OFD1TextureToSplit
        '
        Me.OFD1TextureToSplit.FileName = "Select Texture That You Want To Split"
        '
        'BLPOrreryMainMenuStrip
        '
        Me.BLPOrreryMainMenuStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.FileToolStripMenuItem, Me.ViewToolStripMenuItem, Me.ToolsToolStripMenuItem, Me.ExplorerThumbnailsToolStripMenuItem, Me.AboutToolStripMenuItem})
        Me.BLPOrreryMainMenuStrip.Location = New System.Drawing.Point(0, 0)
        Me.BLPOrreryMainMenuStrip.Name = "BLPOrreryMainMenuStrip"
        Me.BLPOrreryMainMenuStrip.Size = New System.Drawing.Size(710, 24)
        Me.BLPOrreryMainMenuStrip.TabIndex = 44
        Me.BLPOrreryMainMenuStrip.Text = "MenuStrip1"
        '
        'FileToolStripMenuItem
        '
        Me.FileToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.OpenFileToolStripMenuItem, Me.SaveAsToolStripMenuItem, Me.ResizePltToolStripMenuItem, Me.ToolStripSeparator1, Me.QuitToolStripMenuItem})
        Me.FileToolStripMenuItem.Name = "FileToolStripMenuItem"
        Me.FileToolStripMenuItem.Size = New System.Drawing.Size(37, 20)
        Me.FileToolStripMenuItem.Text = "File"
        '
        'OpenFileToolStripMenuItem
        '
        Me.OpenFileToolStripMenuItem.Name = "OpenFileToolStripMenuItem"
        Me.OpenFileToolStripMenuItem.ShortcutKeys = CType((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.O), System.Windows.Forms.Keys)
        Me.OpenFileToolStripMenuItem.Size = New System.Drawing.Size(205, 22)
        Me.OpenFileToolStripMenuItem.Text = "Open File"
        '
        'SaveAsToolStripMenuItem
        '
        Me.SaveAsToolStripMenuItem.Name = "SaveAsToolStripMenuItem"
        Me.SaveAsToolStripMenuItem.ShortcutKeys = CType(((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.Shift) _
            Or System.Windows.Forms.Keys.S), System.Windows.Forms.Keys)
        Me.SaveAsToolStripMenuItem.Size = New System.Drawing.Size(205, 22)
        Me.SaveAsToolStripMenuItem.Text = "Save As ..."
        '
        'ResizePltToolStripMenuItem
        '
        Me.ResizePltToolStripMenuItem.Enabled = False
        Me.ResizePltToolStripMenuItem.Name = "ResizePltToolStripMenuItem"
        Me.ResizePltToolStripMenuItem.Size = New System.Drawing.Size(205, 22)
        Me.ResizePltToolStripMenuItem.Text = "Resize Image ..."
        '
        'ToolStripSeparator1
        '
        Me.ToolStripSeparator1.Name = "ToolStripSeparator1"
        Me.ToolStripSeparator1.Size = New System.Drawing.Size(202, 6)
        '
        'QuitToolStripMenuItem
        '
        Me.QuitToolStripMenuItem.Name = "QuitToolStripMenuItem"
        Me.QuitToolStripMenuItem.Size = New System.Drawing.Size(205, 22)
        Me.QuitToolStripMenuItem.Text = "Quit"
        '
        'ViewToolStripMenuItem
        '
        Me.ViewToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.FileInformationsToolStripMenuItem})
        Me.ViewToolStripMenuItem.Name = "ViewToolStripMenuItem"
        Me.ViewToolStripMenuItem.Size = New System.Drawing.Size(44, 20)
        Me.ViewToolStripMenuItem.Text = "View"
        '
        'FileInformationsToolStripMenuItem
        '
        Me.FileInformationsToolStripMenuItem.Checked = True
        Me.FileInformationsToolStripMenuItem.CheckOnClick = True
        Me.FileInformationsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked
        Me.FileInformationsToolStripMenuItem.Name = "FileInformationsToolStripMenuItem"
        Me.FileInformationsToolStripMenuItem.Size = New System.Drawing.Size(163, 22)
        Me.FileInformationsToolStripMenuItem.Text = "File Informations"
        '
        'ToolsToolStripMenuItem
        '
        Me.ToolsToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.RenderTransparencyMenuItem, Me.ResizeByTextureMenuItem, Me.PreviewActualSizeMenuItem, Me.FlipBioWareDdsMenuItem})
        Me.ToolsToolStripMenuItem.Name = "ToolsToolStripMenuItem"
        Me.ToolsToolStripMenuItem.Size = New System.Drawing.Size(61, 20)
        Me.ToolsToolStripMenuItem.Text = "Settings"
        '
        'RenderTransparencyMenuItem
        '
        Me.RenderTransparencyMenuItem.CheckOnClick = True
        Me.RenderTransparencyMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.BackgroundClrMenuItem})
        Me.RenderTransparencyMenuItem.Name = "RenderTransparencyMenuItem"
        Me.RenderTransparencyMenuItem.Size = New System.Drawing.Size(183, 22)
        Me.RenderTransparencyMenuItem.Text = "Render Transparency"
        '
        'BackgroundClrMenuItem
        '
        Me.BackgroundClrMenuItem.ForeColor = System.Drawing.Color.Black
        Me.BackgroundClrMenuItem.Name = "BackgroundClrMenuItem"
        Me.BackgroundClrMenuItem.Size = New System.Drawing.Size(193, 22)
        Me.BackgroundClrMenuItem.Text = "▋▋▋▋▋▋ Color ▋▋▋▋▋▋"
        '
        'ResizeByTextureMenuItem
        '
        Me.ResizeByTextureMenuItem.CheckOnClick = True
        Me.ResizeByTextureMenuItem.Name = "ResizeByTextureMenuItem"
        Me.ResizeByTextureMenuItem.Size = New System.Drawing.Size(183, 22)
        Me.ResizeByTextureMenuItem.Text = "Resize By Texture"
        '
        'PreviewActualSizeMenuItem
        '
        Me.PreviewActualSizeMenuItem.CheckOnClick = True
        Me.PreviewActualSizeMenuItem.Name = "PreviewActualSizeMenuItem"
        Me.PreviewActualSizeMenuItem.Size = New System.Drawing.Size(183, 22)
        Me.PreviewActualSizeMenuItem.Text = "Preview Actual Size"
        '
        'FlipBioWareDdsMenuItem
        '
        Me.FlipBioWareDdsMenuItem.CheckOnClick = True
        Me.FlipBioWareDdsMenuItem.Name = "FlipBioWareDdsMenuItem"
        Me.FlipBioWareDdsMenuItem.Size = New System.Drawing.Size(183, 22)
        Me.FlipBioWareDdsMenuItem.Text = "Flip BioWare DDS"
        '
        'ExplorerThumbnailsToolStripMenuItem
        '
        Me.ExplorerThumbnailsToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ShellStatusMenuItem, Me.ShellEnableMenuItem, Me.ShellFormatsMenuItem, Me.ShellMenuSeparator1, Me.ShellApplyMenuItem, Me.ShellRefreshMenuItem, Me.ShellDetailsMenuItem, Me.ShellOpenFolderMenuItem})
        Me.ExplorerThumbnailsToolStripMenuItem.Name = "ExplorerThumbnailsToolStripMenuItem"
        Me.ExplorerThumbnailsToolStripMenuItem.Size = New System.Drawing.Size(128, 20)
        Me.ExplorerThumbnailsToolStripMenuItem.Text = "Explorer Thumbnails"
        '
        'ShellStatusMenuItem
        '
        Me.ShellStatusMenuItem.Enabled = False
        Me.ShellStatusMenuItem.Name = "ShellStatusMenuItem"
        Me.ShellStatusMenuItem.Size = New System.Drawing.Size(270, 22)
        Me.ShellStatusMenuItem.Text = "Status: checking..."
        '
        'ShellEnableMenuItem
        '
        Me.ShellEnableMenuItem.CheckOnClick = True
        Me.ShellEnableMenuItem.Name = "ShellEnableMenuItem"
        Me.ShellEnableMenuItem.Size = New System.Drawing.Size(270, 22)
        Me.ShellEnableMenuItem.Text = "Enable Orrery thumbnails"
        '
        'ShellFormatsMenuItem
        '
        Me.ShellFormatsMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ShellBlpMenuItem, Me.ShellDdsMenuItem, Me.ShellPltMenuItem, Me.ShellIcoMenuItem})
        Me.ShellFormatsMenuItem.Name = "ShellFormatsMenuItem"
        Me.ShellFormatsMenuItem.Size = New System.Drawing.Size(270, 22)
        Me.ShellFormatsMenuItem.Text = "Use Orrery for"
        '
        'ShellBlpMenuItem
        '
        Me.ShellBlpMenuItem.CheckOnClick = True
        Me.ShellBlpMenuItem.Name = "ShellBlpMenuItem"
        Me.ShellBlpMenuItem.Size = New System.Drawing.Size(284, 22)
        Me.ShellBlpMenuItem.Text = ".BLP - Blizzard textures"
        '
        'ShellDdsMenuItem
        '
        Me.ShellDdsMenuItem.CheckOnClick = True
        Me.ShellDdsMenuItem.Name = "ShellDdsMenuItem"
        Me.ShellDdsMenuItem.Size = New System.Drawing.Size(284, 22)
        Me.ShellDdsMenuItem.Text = ".DDS - DDS and BioWare/NWN textures"
        '
        'ShellPltMenuItem
        '
        Me.ShellPltMenuItem.CheckOnClick = True
        Me.ShellPltMenuItem.Name = "ShellPltMenuItem"
        Me.ShellPltMenuItem.Size = New System.Drawing.Size(284, 22)
        Me.ShellPltMenuItem.Text = ".PLT - Neverwinter Nights layers"
        '
        'ShellIcoMenuItem
        '
        Me.ShellIcoMenuItem.CheckOnClick = True
        Me.ShellIcoMenuItem.Name = "ShellIcoMenuItem"
        Me.ShellIcoMenuItem.Size = New System.Drawing.Size(284, 22)
        Me.ShellIcoMenuItem.Text = ".ICO - Windows icons"
        '
        'ShellMenuSeparator1
        '
        Me.ShellMenuSeparator1.Name = "ShellMenuSeparator1"
        Me.ShellMenuSeparator1.Size = New System.Drawing.Size(267, 6)
        '
        'ShellApplyMenuItem
        '
        Me.ShellApplyMenuItem.Name = "ShellApplyMenuItem"
        Me.ShellApplyMenuItem.Size = New System.Drawing.Size(270, 22)
        Me.ShellApplyMenuItem.Text = "Apply / Update Now"
        '
        'ShellRefreshMenuItem
        '
        Me.ShellRefreshMenuItem.Name = "ShellRefreshMenuItem"
        Me.ShellRefreshMenuItem.Size = New System.Drawing.Size(270, 22)
        Me.ShellRefreshMenuItem.Text = "Refresh Windows Explorer"
        '
        'ShellDetailsMenuItem
        '
        Me.ShellDetailsMenuItem.Name = "ShellDetailsMenuItem"
        Me.ShellDetailsMenuItem.Size = New System.Drawing.Size(270, 22)
        Me.ShellDetailsMenuItem.Text = "Status Details..."
        '
        'ShellOpenFolderMenuItem
        '
        Me.ShellOpenFolderMenuItem.Name = "ShellOpenFolderMenuItem"
        Me.ShellOpenFolderMenuItem.Size = New System.Drawing.Size(270, 22)
        Me.ShellOpenFolderMenuItem.Text = "Open Installed Provider Folder"
        '
        'AboutToolStripMenuItem
        '
        Me.AboutToolStripMenuItem.Name = "AboutToolStripMenuItem"
        Me.AboutToolStripMenuItem.Size = New System.Drawing.Size(52, 20)
        Me.AboutToolStripMenuItem.Text = "About"
        '
        'GrBxFileInfo
        '
        Me.GrBxFileInfo.BackColor = System.Drawing.Color.FromArgb(CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer))
        Me.GrBxFileInfo.Controls.Add(Me.LblActiveMipMaipValue)
        Me.GrBxFileInfo.Controls.Add(Me.LblActiveMipMap)
        Me.GrBxFileInfo.Controls.Add(Me.LblResolutionValue)
        Me.GrBxFileInfo.Controls.Add(Me.LblResolution)
        Me.GrBxFileInfo.Controls.Add(Me.LblMipMapCountValue)
        Me.GrBxFileInfo.Controls.Add(Me.LblAlphaChannelValue)
        Me.GrBxFileInfo.Controls.Add(Me.LblAlphaEncodingValue)
        Me.GrBxFileInfo.Controls.Add(Me.LblCompressionValue)
        Me.GrBxFileInfo.Controls.Add(Me.LsBxMipMapList)
        Me.GrBxFileInfo.Controls.Add(Me.LblMipMapCount)
        Me.GrBxFileInfo.Controls.Add(Me.LblAlphaChannel)
        Me.GrBxFileInfo.Controls.Add(Me.LblAlphaEncoding)
        Me.GrBxFileInfo.Controls.Add(Me.LblCompression)
        Me.GrBxFileInfo.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GrBxFileInfo.Location = New System.Drawing.Point(0, 0)
        Me.GrBxFileInfo.Name = "GrBxFileInfo"
        Me.GrBxFileInfo.Size = New System.Drawing.Size(204, 532)
        Me.GrBxFileInfo.TabIndex = 45
        Me.GrBxFileInfo.TabStop = False
        Me.GrBxFileInfo.Text = "File Information"
        '
        'LblActiveMipMaipValue
        '
        Me.LblActiveMipMaipValue.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.LblActiveMipMaipValue.AutoSize = True
        Me.LblActiveMipMaipValue.Location = New System.Drawing.Point(93, 286)
        Me.LblActiveMipMaipValue.Name = "LblActiveMipMaipValue"
        Me.LblActiveMipMaipValue.Size = New System.Drawing.Size(27, 13)
        Me.LblActiveMipMaipValue.TabIndex = 14
        Me.LblActiveMipMaipValue.Text = "N/A"
        '
        'LblActiveMipMap
        '
        Me.LblActiveMipMap.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.LblActiveMipMap.AutoSize = True
        Me.LblActiveMipMap.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.LblActiveMipMap.Location = New System.Drawing.Point(13, 286)
        Me.LblActiveMipMap.Name = "LblActiveMipMap"
        Me.LblActiveMipMap.Size = New System.Drawing.Size(80, 13)
        Me.LblActiveMipMap.TabIndex = 13
        Me.LblActiveMipMap.Text = "Active Mipmap:"
        '
        'LblResolutionValue
        '
        Me.LblResolutionValue.AutoSize = True
        Me.LblResolutionValue.Location = New System.Drawing.Point(93, 80)
        Me.LblResolutionValue.Name = "LblResolutionValue"
        Me.LblResolutionValue.Size = New System.Drawing.Size(27, 13)
        Me.LblResolutionValue.TabIndex = 12
        Me.LblResolutionValue.Text = "N/A"
        '
        'LblResolution
        '
        Me.LblResolution.AutoSize = True
        Me.LblResolution.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.LblResolution.Location = New System.Drawing.Point(27, 80)
        Me.LblResolution.Name = "LblResolution"
        Me.LblResolution.Size = New System.Drawing.Size(60, 13)
        Me.LblResolution.TabIndex = 11
        Me.LblResolution.Text = "Resolution:"
        '
        'LblMipMapCountValue
        '
        Me.LblMipMapCountValue.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.LblMipMapCountValue.AutoSize = True
        Me.LblMipMapCountValue.Location = New System.Drawing.Point(93, 266)
        Me.LblMipMapCountValue.Name = "LblMipMapCountValue"
        Me.LblMipMapCountValue.Size = New System.Drawing.Size(27, 13)
        Me.LblMipMapCountValue.TabIndex = 10
        Me.LblMipMapCountValue.Text = "N/A"
        '
        'LblAlphaChannelValue
        '
        Me.LblAlphaChannelValue.AutoSize = True
        Me.LblAlphaChannelValue.Location = New System.Drawing.Point(93, 60)
        Me.LblAlphaChannelValue.Name = "LblAlphaChannelValue"
        Me.LblAlphaChannelValue.Size = New System.Drawing.Size(27, 13)
        Me.LblAlphaChannelValue.TabIndex = 9
        Me.LblAlphaChannelValue.Text = "N/A"
        '
        'LblAlphaEncodingValue
        '
        Me.LblAlphaEncodingValue.AutoSize = True
        Me.LblAlphaEncodingValue.Location = New System.Drawing.Point(93, 40)
        Me.LblAlphaEncodingValue.Name = "LblAlphaEncodingValue"
        Me.LblAlphaEncodingValue.Size = New System.Drawing.Size(27, 13)
        Me.LblAlphaEncodingValue.TabIndex = 8
        Me.LblAlphaEncodingValue.Text = "N/A"
        '
        'LblCompressionValue
        '
        Me.LblCompressionValue.AutoSize = True
        Me.LblCompressionValue.Location = New System.Drawing.Point(93, 21)
        Me.LblCompressionValue.Name = "LblCompressionValue"
        Me.LblCompressionValue.Size = New System.Drawing.Size(27, 13)
        Me.LblCompressionValue.TabIndex = 7
        Me.LblCompressionValue.Text = "N/A"
        '
        'LsBxMipMapList
        '
        Me.LsBxMipMapList.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.LsBxMipMapList.Font = New System.Drawing.Font("Consolas", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LsBxMipMapList.FormattingEnabled = True
        Me.LsBxMipMapList.Items.AddRange(New Object() {" #   MapSize    MapOffset"})
        Me.LsBxMipMapList.Location = New System.Drawing.Point(0, 305)
        Me.LsBxMipMapList.Name = "LsBxMipMapList"
        Me.LsBxMipMapList.Size = New System.Drawing.Size(203, 225)
        Me.LsBxMipMapList.TabIndex = 5
        '
        'LblMipMapCount
        '
        Me.LblMipMapCount.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.LblMipMapCount.AutoSize = True
        Me.LblMipMapCount.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.LblMipMapCount.Location = New System.Drawing.Point(15, 266)
        Me.LblMipMapCount.Name = "LblMipMapCount"
        Me.LblMipMapCount.Size = New System.Drawing.Size(78, 13)
        Me.LblMipMapCount.TabIndex = 4
        Me.LblMipMapCount.Text = "Mipmap Count:"
        '
        'LblAlphaChannel
        '
        Me.LblAlphaChannel.AutoSize = True
        Me.LblAlphaChannel.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.LblAlphaChannel.Location = New System.Drawing.Point(8, 60)
        Me.LblAlphaChannel.Name = "LblAlphaChannel"
        Me.LblAlphaChannel.Size = New System.Drawing.Size(79, 13)
        Me.LblAlphaChannel.TabIndex = 3
        Me.LblAlphaChannel.Text = "Alpha Channel:"
        '
        'LblAlphaEncoding
        '
        Me.LblAlphaEncoding.AutoSize = True
        Me.LblAlphaEncoding.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.LblAlphaEncoding.Location = New System.Drawing.Point(2, 40)
        Me.LblAlphaEncoding.Name = "LblAlphaEncoding"
        Me.LblAlphaEncoding.Size = New System.Drawing.Size(85, 13)
        Me.LblAlphaEncoding.TabIndex = 2
        Me.LblAlphaEncoding.Text = "Alpha Encoding:"
        '
        'LblCompression
        '
        Me.LblCompression.AutoSize = True
        Me.LblCompression.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.LblCompression.Location = New System.Drawing.Point(17, 21)
        Me.LblCompression.Name = "LblCompression"
        Me.LblCompression.Size = New System.Drawing.Size(70, 13)
        Me.LblCompression.TabIndex = 1
        Me.LblCompression.Text = "Compression:"
        '
        'PnlToolBar
        '
        Me.PnlToolBar.BackColor = System.Drawing.Color.FromArgb(CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer))
        Me.PnlToolBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PnlToolBar.Controls.Add(Me.BtnPreviousImage)
        Me.PnlToolBar.Controls.Add(Me.BtnNextImage)
        Me.PnlToolBar.Controls.Add(Me.BtnZoomOut)
        Me.PnlToolBar.Controls.Add(Me.BtnZoomIn)
        Me.PnlToolBar.Controls.Add(Me.BtnResizePlt)
        Me.PnlToolBar.Controls.Add(Me.BtnMask)
        Me.PnlToolBar.Controls.Add(Me.BtnTransparency)
        Me.PnlToolBar.Controls.Add(Me.ComBxSaveAsFormat)
        Me.PnlToolBar.Controls.Add(Me.BtnSave)
        Me.PnlToolBar.Controls.Add(Me.TxBxTextureDirectory)
        Me.PnlToolBar.Controls.Add(Me.TxBxTextureName)
        Me.PnlToolBar.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.PnlToolBar.Location = New System.Drawing.Point(0, 443)
        Me.PnlToolBar.Name = "PnlToolBar"
        Me.PnlToolBar.Size = New System.Drawing.Size(502, 89)
        Me.PnlToolBar.TabIndex = 46
        '
        'BtnPreviousImage
        '
        Me.BtnPreviousImage.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.BtnPreviousImage.BackColor = System.Drawing.Color.Silver
        Me.BtnPreviousImage.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.BtnPreviousImage.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.BtnPreviousImage.Location = New System.Drawing.Point(4, 52)
        Me.BtnPreviousImage.Name = "BtnPreviousImage"
        Me.BtnPreviousImage.Size = New System.Drawing.Size(32, 32)
        Me.BtnPreviousImage.TabIndex = 15
        Me.BtnPreviousImage.Text = "<"
        Me.BtnPreviousImage.UseVisualStyleBackColor = False
        '
        'BtnNextImage
        '
        Me.BtnNextImage.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.BtnNextImage.BackColor = System.Drawing.Color.Silver
        Me.BtnNextImage.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.BtnNextImage.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.BtnNextImage.Location = New System.Drawing.Point(42, 52)
        Me.BtnNextImage.Name = "BtnNextImage"
        Me.BtnNextImage.Size = New System.Drawing.Size(32, 32)
        Me.BtnNextImage.TabIndex = 16
        Me.BtnNextImage.Text = ">"
        Me.BtnNextImage.UseVisualStyleBackColor = False
        '
        'BtnZoomOut
        '
        Me.BtnZoomOut.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.BtnZoomOut.BackColor = System.Drawing.Color.Silver
        Me.BtnZoomOut.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.BtnZoomOut.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.BtnZoomOut.Location = New System.Drawing.Point(80, 52)
        Me.BtnZoomOut.Name = "BtnZoomOut"
        Me.BtnZoomOut.Size = New System.Drawing.Size(32, 32)
        Me.BtnZoomOut.TabIndex = 17
        Me.BtnZoomOut.Text = "-"
        Me.BtnZoomOut.UseVisualStyleBackColor = False
        '
        'BtnZoomIn
        '
        Me.BtnZoomIn.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.BtnZoomIn.BackColor = System.Drawing.Color.Silver
        Me.BtnZoomIn.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.BtnZoomIn.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.BtnZoomIn.Location = New System.Drawing.Point(118, 52)
        Me.BtnZoomIn.Name = "BtnZoomIn"
        Me.BtnZoomIn.Size = New System.Drawing.Size(32, 32)
        Me.BtnZoomIn.TabIndex = 18
        Me.BtnZoomIn.Text = "+"
        Me.BtnZoomIn.UseVisualStyleBackColor = False
        '
        'BtnResizePlt
        '
        Me.BtnResizePlt.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.BtnResizePlt.BackColor = System.Drawing.Color.Silver
        Me.BtnResizePlt.Enabled = False
        Me.BtnResizePlt.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.BtnResizePlt.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.BtnResizePlt.Location = New System.Drawing.Point(156, 52)
        Me.BtnResizePlt.Name = "BtnResizePlt"
        Me.BtnResizePlt.Size = New System.Drawing.Size(32, 32)
        Me.BtnResizePlt.TabIndex = 19
        Me.BtnResizePlt.Text = "R"
        Me.BtnResizePlt.UseVisualStyleBackColor = False
        '
        'BtnMask
        '
        Me.BtnMask.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.BtnMask.AutoSize = True
        Me.BtnMask.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.BtnMask.BackColor = System.Drawing.Color.Silver
        Me.BtnMask.Image = CType(resources.GetObject("BtnMask.Image"), System.Drawing.Image)
        Me.BtnMask.Location = New System.Drawing.Point(313, 24)
        Me.BtnMask.Margin = New System.Windows.Forms.Padding(0)
        Me.BtnMask.Name = "BtnMask"
        Me.BtnMask.Size = New System.Drawing.Size(56, 56)
        Me.BtnMask.TabIndex = 46
        Me.BtnMask.UseVisualStyleBackColor = False
        '
        'BtnTransparency
        '
        Me.BtnTransparency.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.BtnTransparency.AutoSize = True
        Me.BtnTransparency.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.BtnTransparency.BackColor = System.Drawing.Color.Silver
        Me.BtnTransparency.Image = CType(resources.GetObject("BtnTransparency.Image"), System.Drawing.Image)
        Me.BtnTransparency.Location = New System.Drawing.Point(369, 24)
        Me.BtnTransparency.Margin = New System.Windows.Forms.Padding(0)
        Me.BtnTransparency.Name = "BtnTransparency"
        Me.BtnTransparency.Size = New System.Drawing.Size(56, 56)
        Me.BtnTransparency.TabIndex = 45
        Me.BtnTransparency.UseVisualStyleBackColor = False
        '
        'ComBxSaveAsFormat
        '
        Me.ComBxSaveAsFormat.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ComBxSaveAsFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComBxSaveAsFormat.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.ComBxSaveAsFormat.FormattingEnabled = True
        Me.ComBxSaveAsFormat.Items.AddRange(New Object() {"PNG", "JPG"})
        Me.ComBxSaveAsFormat.Location = New System.Drawing.Point(428, 53)
        Me.ComBxSaveAsFormat.Name = "ComBxSaveAsFormat"
        Me.ComBxSaveAsFormat.Size = New System.Drawing.Size(69, 24)
        Me.ComBxSaveAsFormat.TabIndex = 44
        '
        'PnlFileInfo
        '
        Me.PnlFileInfo.BackColor = System.Drawing.Color.FromArgb(CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer))
        Me.PnlFileInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PnlFileInfo.Controls.Add(Me.GrBxFileInfo)
        Me.PnlFileInfo.Dock = System.Windows.Forms.DockStyle.Left
        Me.PnlFileInfo.Location = New System.Drawing.Point(0, 24)
        Me.PnlFileInfo.Name = "PnlFileInfo"
        Me.PnlFileInfo.Size = New System.Drawing.Size(206, 534)
        Me.PnlFileInfo.TabIndex = 47
        '
        'PnlTextureViewAndToolBar
        '
        Me.PnlTextureViewAndToolBar.BackColor = System.Drawing.Color.FromArgb(CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer))
        Me.PnlTextureViewAndToolBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PnlTextureViewAndToolBar.Controls.Add(Me.PnlTextureView)
        Me.PnlTextureViewAndToolBar.Controls.Add(Me.PnlToolBar)
        Me.PnlTextureViewAndToolBar.Dock = System.Windows.Forms.DockStyle.Fill
        Me.PnlTextureViewAndToolBar.Location = New System.Drawing.Point(206, 24)
        Me.PnlTextureViewAndToolBar.Name = "PnlTextureViewAndToolBar"
        Me.PnlTextureViewAndToolBar.Size = New System.Drawing.Size(504, 534)
        Me.PnlTextureViewAndToolBar.TabIndex = 48
        '
        'PnlTextureView
        '
        Me.PnlTextureView.AutoScroll = True
        Me.PnlTextureView.BackColor = System.Drawing.Color.FromArgb(CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer))
        Me.PnlTextureView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PnlTextureView.Controls.Add(Me.PicBxTextureView)
        Me.PnlTextureView.Dock = System.Windows.Forms.DockStyle.Fill
        Me.PnlTextureView.Location = New System.Drawing.Point(0, 0)
        Me.PnlTextureView.Name = "PnlTextureView"
        Me.PnlTextureView.Size = New System.Drawing.Size(502, 443)
        Me.PnlTextureView.TabIndex = 47
        '
        'BLP_Orrery_MainForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.ButtonShadow
        Me.ClientSize = New System.Drawing.Size(710, 558)
        Me.Controls.Add(Me.PnlTextureViewAndToolBar)
        Me.Controls.Add(Me.PnlFileInfo)
        Me.Controls.Add(Me.BLPOrreryMainMenuStrip)
        Me.DataBindings.Add(New System.Windows.Forms.Binding("Location", Global.BLP_Orrery.My.MySettings.Default, "WindowPoint", True, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged))
        Me.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Location = Global.BLP_Orrery.My.MySettings.Default.WindowPoint
        Me.MainMenuStrip = Me.BLPOrreryMainMenuStrip
        Me.MinimumSize = New System.Drawing.Size(585, 430)
        Me.Name = "BLP_Orrery_MainForm"
        Me.RightToLeft = System.Windows.Forms.RightToLeft.No
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "BLP Orrery"
        CType(Me.PicBxTextureView, System.ComponentModel.ISupportInitialize).EndInit()
        Me.BLPOrreryMainMenuStrip.ResumeLayout(False)
        Me.BLPOrreryMainMenuStrip.PerformLayout()
        Me.GrBxFileInfo.ResumeLayout(False)
        Me.GrBxFileInfo.PerformLayout()
        Me.PnlToolBar.ResumeLayout(False)
        Me.PnlToolBar.PerformLayout()
        Me.PnlFileInfo.ResumeLayout(False)
        Me.PnlTextureViewAndToolBar.ResumeLayout(False)
        Me.PnlTextureView.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents TxBxTextureDirectory As System.Windows.Forms.TextBox
    Friend WithEvents PicBxTextureView As System.Windows.Forms.PictureBox
    Friend WithEvents BtnSave As System.Windows.Forms.Button
    Friend WithEvents TxBxTextureName As System.Windows.Forms.TextBox
    Friend WithEvents OFD1TextureToSplit As System.Windows.Forms.OpenFileDialog
    Friend WithEvents BLPOrreryMainMenuStrip As MenuStrip
    Friend WithEvents FileToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ViewToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents AboutToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents GrBxFileInfo As GroupBox
    Friend WithEvents PnlToolBar As Panel
    Friend WithEvents ComBxSaveAsFormat As ComboBox
    Friend WithEvents OpenFileToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SaveAsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ResizePltToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator1 As ToolStripSeparator
    Friend WithEvents QuitToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents FileInformationsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents LblCompression As Label
    Friend WithEvents LblAlphaChannel As Label
    Friend WithEvents LblAlphaEncoding As Label
    Friend WithEvents LsBxMipMapList As ListBox
    Friend WithEvents LblAlphaChannelValue As Label
    Friend WithEvents LblAlphaEncodingValue As Label
    Friend WithEvents LblCompressionValue As Label
    Friend WithEvents ToolsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents RenderTransparencyMenuItem As ToolStripMenuItem
    Friend WithEvents LblMipMapCountValue As Label
    Friend WithEvents LblMipMapCount As Label
    Friend WithEvents BtnTransparency As Button
    Friend WithEvents BtnMask As Button
    Friend WithEvents PnlFileInfo As Panel
    Friend WithEvents PnlTextureViewAndToolBar As Panel
    Friend WithEvents PnlTextureView As Panel
    Friend WithEvents ResizeByTextureMenuItem As ToolStripMenuItem
    Friend WithEvents PreviewActualSizeMenuItem As ToolStripMenuItem
    Friend WithEvents FlipBioWareDdsMenuItem As ToolStripMenuItem
    Friend WithEvents ExplorerThumbnailsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ShellStatusMenuItem As ToolStripMenuItem
    Friend WithEvents ShellEnableMenuItem As ToolStripMenuItem
    Friend WithEvents ShellFormatsMenuItem As ToolStripMenuItem
    Friend WithEvents ShellBlpMenuItem As ToolStripMenuItem
    Friend WithEvents ShellDdsMenuItem As ToolStripMenuItem
    Friend WithEvents ShellPltMenuItem As ToolStripMenuItem
    Friend WithEvents ShellIcoMenuItem As ToolStripMenuItem
    Friend WithEvents ShellMenuSeparator1 As ToolStripSeparator
    Friend WithEvents ShellApplyMenuItem As ToolStripMenuItem
    Friend WithEvents ShellRefreshMenuItem As ToolStripMenuItem
    Friend WithEvents ShellDetailsMenuItem As ToolStripMenuItem
    Friend WithEvents ShellOpenFolderMenuItem As ToolStripMenuItem
    Friend WithEvents BackgroundClrMenuItem As ToolStripMenuItem
    Friend WithEvents ClrPickBackGround As ColorDialog
    Friend WithEvents LblResolutionValue As Label
    Friend WithEvents LblResolution As Label
    Friend WithEvents LblActiveMipMaipValue As Label
    Friend WithEvents LblActiveMipMap As Label
    Friend WithEvents BtnPreviousImage As Button
    Friend WithEvents BtnNextImage As Button
    Friend WithEvents BtnZoomOut As Button
    Friend WithEvents BtnZoomIn As Button
    Friend WithEvents BtnResizePlt As Button
End Class
