using custom_folder_browser;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace CustomFolder
{
    public partial class CustomFolderBrowser : Form
    {
        private TreeView folderTreeView;
        private Button okButton;
        private Button cancelButton;
        private Button makeNewFolderButton;
        private BufferedLabel feedbackLabel;

        public string SelectedPath { get; private set; }

        public CustomFolderBrowser()
        {
            InitializeComponents();
            ClientSize = new Size(578, 744);
            okButton.Enabled = false;
            this.StartPosition = FormStartPosition.CenterScreen;

#if false
            // Load the icons from resources (make sure these are 16x16 size)
            expandIcon = Properties.Resources.icons8_forward_8; // Right-facing arrow
            collapseIcon = Properties.Resources.icons8_expand_arrow_8; // Down-facing arrow

#endif

            // Enable owner drawing for the TreeView in DoubleBufferredTreeView class

            this.Resize += CustomFolderBrowser_Resize;
            this.Move += CustomFolderBrowser_Move;

            LoadDrives();
        }

        private void CustomFolderBrowser_Move(object sender, EventArgs e)
        {
            UpdateFeedbackLabel();
        }

        private void CustomFolderBrowser_Resize(object sender, EventArgs e)
        {
            UpdateFeedbackLabel();
        }

        private void UpdateFeedbackLabel()
        {
            // Code to update move and resize feedback, if necessary
        }

        private void InitializeComponents()
        {
            this.Text = "Select Folder";
            this.Size = new Size(332, 349);

            folderTreeView = new DoubleBufferedTreeView
            {
                Size = new Size(290, 208),
                Location = new Point(12, 55),
                Dock = DockStyle.Fill,
            };

            folderTreeView.AfterSelect += FolderTreeView_AfterSelect;
            folderTreeView.BeforeExpand += FolderTreeView_BeforeExpand;

            okButton = new Button
            {
                Text = "Ok",
                Size = new Size(75, 23),
                Location = new Point(147, 280)
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(75, 23),
                Location = new Point(227, 280)
            };
            cancelButton.Click += (s, e) => this.Close();

            makeNewFolderButton = new Button
            {
                Text = "Make New Folder",
                Size = new Size(120, 23),
                Location = new Point(12, 280)
            };

            feedbackLabel = new BufferedLabel
            {
                Text = string.Empty,
                ForeColor = Color.Red,
                Size = new Size(300, 20),
                Location = new Point(12, 25)
            };

            Controls.Add(folderTreeView);
            //Controls.Add(feedbackLabel);
            //Controls.Add(okButton);
            //Controls.Add(cancelButton);
            //Controls.Add(makeNewFolderButton);
        }

        private void LoadDrives()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                TreeNode node = new TreeNode(drive.Name) { Tag = drive.RootDirectory.FullName };
                folderTreeView.Nodes.Add(node);
                node.Nodes.Add("Loading..."); // Placeholder for expandable drives
            }

            feedbackLabel.Select();
        }

        private void FolderTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode node = e.Node;
            if (node.Nodes[0].Text == "Loading...")
            {
                node.Nodes.Clear();
                LoadSubDirectories(node);
            }
        }

        private void LoadSubDirectories(TreeNode node)
        {
            string path = (string)node.Tag;
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                foreach (var directory in directoryInfo.GetDirectories())
                {
                    TreeNode subNode = new TreeNode(directory.Name) { Tag = directory.FullName };
                    node.Nodes.Add(subNode);
                    subNode.Nodes.Add("Loading...");
                    okButton.Enabled = true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                feedbackLabel.Text = "Access Denied to folder.";
                okButton.Enabled = false;
            }
        }

        private void FolderTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string selectedPath = (string)e.Node.Tag;
            DirectoryInfo directoryInfo = new DirectoryInfo(selectedPath);
            try
            {
                var subDirs = directoryInfo.GetDirectories();
                feedbackLabel.Text = string.Empty;
                SelectedPath = selectedPath;
                okButton.Enabled = true;
            }
            catch (UnauthorizedAccessException)
            {
                feedbackLabel.Text = "Folder Access Denied.";
                okButton.Enabled = false;
            }
        }
        bool _subScribed = false;

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(SelectedPath))
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void UpdateTreeView()
        {
            folderTreeView.BeginUpdate();
            try
            {
                // Perform updates to the TreeView here
            }
            finally
            {
                folderTreeView.EndUpdate();
            }
        }

        public class BufferedLabel : Label
        {
            public BufferedLabel()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                this.SetStyle(ControlStyles.UserPaint, true);
            }
        }
    }

    public class DoubleBufferedTreeView : TreeView
    {

#if !USE_FONTELLO
            private Image expandIcon;
            private Image collapseIcon;
#endif
        public DoubleBufferedTreeView()
        {
            // Enable double buffering
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
            DrawMode = TreeViewDrawMode.OwnerDrawAll;
#if USE_FONTELLO
            IconFontFamily = LoadFamilyFromEmbeddedFont("fontello-custom-plusminus-fonts.ttf");
#endif
        }

        protected override void WndProc(ref Message m)
        {
            // Suppress background erase to reduce flickering
            if (m.Msg == 0x0014) // WM_ERASEBKGND
                return;

            base.WndProc(ref m);
        }
        public FontFamily IconFontFamily { get; }
        protected override void OnDrawNode(DrawTreeNodeEventArgs e)
        {
            if (e.Node is null || e.Bounds.Width == 0 || e.Bounds.Height == 0)
            {
                e.DrawDefault = true;
                base.OnDrawNode(e);
                return;
            }
            // Get the bounds of the node
            var metrics = GetNodeMetrics(e);
            Rectangle nodeRect = e.Node.Bounds;

            // Clear the icon area before redrawing
            e.Graphics.FillRectangle(Brushes.Azure, e.Bounds);  // Clear previous drawing

            /*--------- 1. Draw expand/collapse icon ---------*/
            if (e.Node.Nodes.Count > 0)
            {
                nodeRect.X = metrics.IconBounds.Left;
#if USE_FONTELLO
                localDrawIcon(
                    glyph: e.Node.IsExpanded ? "\uE800" : "\uE801",
                    fontFamily: "fontello-custom-plusminus-fonts",
                    bounds: new(
                        new Point(e.Node.Level * Indent, nodeRect.Top),
                        new Size(metrics.IconBounds.Width, nodeRect.Height)),
                    foreColor_: Color.Black);
#else
                    // Calculate position for expand/collapse icon
                    Point ptExpand = new Point(nodeRect.Left - 16, nodeRect.Top + (nodeRect.Height - 16) / 2); // Align vertically

                // Draw the icon (ensure it is redrawn over a clear background)
                // Choose the appropriate icon
                Image expandCollapseImg = e.Node.IsExpanded ? collapseIcon : expandIcon;
                e.Graphics.DrawImage(expandCollapseImg, ptExpand);
#endif
            }
            return;

            /*--------- 2. Draw node text ---------*/
            // Get the node's font (default if none is set)
            Font nodeFont = e.Node.NodeFont ?? Font;

            // Set the color for the text (highlight if selected)
            Brush textBrush = SystemBrushes.WindowText;
            SizeF stringSize = e.Graphics.MeasureString(
                                e.Node.Text,
                                e.Node.NodeFont ?? Font);
            int
                    stringWidth = Convert.ToInt32(Math.Ceiling(e.Graphics.MeasureString(
                                e.Node.Text,
                                e.Node.NodeFont ?? Font
                                ).Width)),
                    stringHeight = Convert.ToInt32(Math.Ceiling(e.Graphics.MeasureString(
                                e.Node.Text,
                                e.Node.NodeFont ?? Font
                                ).Height));
            switch (e.Node.Text)
            {
                case string s when s.Contains("Program Files"):
                    // Debug.Assert(nodeRect.Width >= stringWidth, $"Should be >= {stringWidth} Is {nodeRect.Width}" );
                    // Debug.Assert(nodeRect.Height >= stringHeight, $"Should be >= {stringHeight} Is {nodeRect.Height}" );
                    break;
            }
            if ((e.State & TreeNodeStates.Selected) != 0)
            {
                textBrush = SystemBrushes.HighlightText;
                e.Graphics.FillRectangle(SystemBrushes.Highlight, nodeRect); // Highlight background
            }
            nodeRect.Width = stringWidth;
            nodeRect.Height = stringHeight;

            // Draw the text
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            e.Graphics.DrawString(e.Node.Text, nodeFont, textBrush, nodeRect);

            // Draw focus rectangle if node is selected
            if ((e.State & TreeNodeStates.Focused) != 0)
            {
                ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds);
            }

            #region L o c a l M e t h o d s
            int localDrawIcon(
                    string glyph,
                    string fontFamily,
                    Rectangle bounds,
                    Color? foreColor_ = null,
                    Color? backColor_ = null,
                    Padding? padding_ = null
                )
            {
                if (Path.GetExtension(fontFamily) != ".ttf")
                {
                    fontFamily = $"{fontFamily}.ttf";
                }
                var ambientFont = e.Node.NodeFont ?? Font;
                using (var iconFont = new Font(LoadFamilyFromEmbeddedFont(fontFamily), ambientFont.Size, ambientFont.Style))
                {
                    var foreColor = foreColor_ ?? ForeColor;
                    var backColor = backColor_ ?? BackColor;
                    var padding = padding_ ?? new Padding(0);
                    var iconBitmap = new Bitmap(bounds.Width, bounds.Height);
                    var drawRect = new Rectangle(
                        new Point(padding.Left, padding.Top),
                        new Size(bounds.Width - padding.Horizontal, bounds.Height - padding.Vertical));

                    using (var iconGraphics = Graphics.FromImage(iconBitmap))
                    using (var stringFormat = new StringFormat(StringFormat.GenericTypographic))
                    using (var foreBrush = new SolidBrush(foreColor))
                    {
                        stringFormat.Alignment = stringFormat.LineAlignment = StringAlignment.Center;

                        iconGraphics.Clear(Color.Transparent);
                        iconGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                        iconGraphics.DrawString(glyph, iconFont, foreBrush, drawRect, stringFormat);
                    }
                    e.Graphics.DrawImage(iconBitmap, bounds.Location);
                }
                return bounds.Width;
            }
            #endregion L o c a l M e t h o d s
        }
        class NodeMetrics
        {
            /// <summary>
            /// X Coodinate where hit begins
            /// </summary>
            //public int Indent { get; init; }
            //public int PlusMinusIndent { get; init; }
            //public int LabelIndent { get; init; }
            //public int RightOfLabelIndent { get; init; }
            //public int IconWidth { get; init; }
            //public int TextWidth { get; init; }
            public Rectangle IconBounds { get; init; }
            public Rectangle LabelBounds { get; init; }
        }
        NodeMetrics GetNodeMetrics(DrawTreeNodeEventArgs? e)
        {
            if (e?.Node is null)
            {
                return new NodeMetrics();
            }
            var node = e.Node;
            var ambientFont = node.NodeFont ?? Font;
            using (var iconFont = new Font(IconFontFamily, ambientFont.Size, ambientFont.Style))
            {
                int
                    indent = node.Level * node.TreeView.Width,
                    iconWidth = Convert.ToInt32(
                        Math.Ceiling(
                            e.Graphics.MeasureString(
                                "\uE800",
                                iconFont
                            ).Width)),
                    textWidth = Convert.ToInt32(
                Math.Ceiling(
                            e.Graphics.MeasureString(
                                node.Text,
                                ambientFont
                            ).Width)),
                    plusMinusIndent = indent + iconWidth,
                    labelIndent = plusMinusIndent + iconWidth,
                    rightOfLabelIndent = labelIndent + textWidth,
                    top = node.Index * ItemHeight;
                Debug.Assert(iconWidth > 0);
                return new NodeMetrics
                {
                    //Indent = indent,
                    //PlusMinusIndent = plusMinusIndent,
                    //LabelIndent = 0,
                    //RightOfLabelIndent = 0,
                    //IconWidth = iconWidth,
                    //TextWidth = textWidth,
                    IconBounds = new Rectangle(
                        x: indent,
                        y: top,
                        width: iconWidth,
                        height: node.Bounds.Height),
                    LabelBounds = new Rectangle(
                        x: plusMinusIndent,
                        y: top,
                        width: textWidth,
                        height: ItemHeight),
                };
            }
        }
#if USE_FONTELLO
        TreeViewHitTestInfo _customHitTestInfo;
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _customHitTestInfo = HitTest(e.Location);
            Debug.WriteLine($"{_customHitTestInfo.Location}");
        }
        public new TreeViewHitTestInfo HitTest(Point point)
        {
            TreeViewHitTestLocations location;
            var bcHitTestInfo = base.HitTest(point);
            if (bcHitTestInfo.Node == null)
            {
                return bcHitTestInfo;
            }
            else
            {
                using (var graphics = CreateGraphics())
                {
                    var metrics = GetNodeMetrics(new DrawTreeNodeEventArgs(
                        graphics: graphics,
                        node: bcHitTestInfo.Node,
                        bounds: new(),
                        state: TreeNodeStates.Default
                    ));
                    switch (point.X)
                    {
                        case int x when x < metrics.IconBounds.Left:
                            location = TreeViewHitTestLocations.LeftOfClientArea;
                            break;
                        case int x when x < metrics.LabelBounds.Left:
                            location = TreeViewHitTestLocations.PlusMinus;
                            break;
                        default:
                            location = TreeViewHitTestLocations.None;
                            break;
                    }
                    return new TreeViewHitTestInfo(GetNodeAt(point), location);
                }
            }
        }

        PrivateFontCollection _privateFontCollection = new PrivateFontCollection();
        FontFamily LoadFamilyFromEmbeddedFont(string ttf)
        {
            var maxLength = Math.Min(ttf.Length, 31);
            var ttfMax = ttf.Substring(0, maxLength);
            var asm = typeof(MainForm).Assembly;
            var v = GetType().Assembly.GetManifestResourceNames();
            var fontFamily = _privateFontCollection.Families.FirstOrDefault(_ => _.Name.Equals(ttfMax));
            if (fontFamily == null) // ... still
            {
                var resourceName = asm.GetManifestResourceNames().FirstOrDefault(_ => _.Contains(ttfMax));
                if (string.IsNullOrWhiteSpace(resourceName))
                {
                    throw new InvalidOperationException("Expecting font file is embedded resource.");
                }
                else
                {
                    using (Stream fontStream = asm.GetManifestResourceStream(resourceName)!)
                    {
                        if (fontStream == null)
                        {
                            throw new InvalidOperationException($"Font resource '{resourceName}' not found.");
                        }
                        else
                        {
                            byte[] fontData = new byte[fontStream.Length];
                            fontStream.Read(fontData, 0, (int)fontStream.Length);

                            IntPtr fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
                            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
                            _privateFontCollection.AddMemoryFont(fontPtr, fontData.Length);

                            fontFamily = _privateFontCollection.Families.First(_ => _.Name.Equals(ttfMax));
                        }
                    }
                }
            }
            return fontFamily;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _privateFontCollection.Dispose();
        }
#endif
    }
    static partial class Extensions
    {
        public class NodeWidthMetrics
        {
            /// <summary>
            /// X Coodinate where hit begins
            /// </summary>
            public int Indent { get; init; }
            public int PlusMinusIndent { get; init; }
            public int LabelIndent { get; init; }
            public int RightOfLabelIndent { get; init; }
            public int IconWidth { get; init; }
            public int TextWidth { get; init; }
            public Rectangle IconBounds { get; init; }
            public Rectangle TextBounds { get; init; }
        }
    }
}