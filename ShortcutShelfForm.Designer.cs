// ShortcutShelfForm.Designer.cs
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ShortcutShelf
{
    partial class ShortcutShelfForm
    {
        private IContainer components = null;

        private TableLayoutPanel tableLayoutPanel1;
        private TextBox txtPath;
        private SplitContainer splitContainer1;
        private ListBox lbShortcuts;
        private Panel filterPanel;
        private Label lblFilter;
        private TextBox txtFilter;
        private ListView lvShortcuts;
        private ImageList imageListLarge;
        private RichTextBox rtbLog;
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem deleteToolStripMenuItem;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new Container();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(ShortcutShelfForm));
            tableLayoutPanel1 = new TableLayoutPanel();
            txtPath = new TextBox();
            splitContainer1 = new SplitContainer();
            lbShortcuts = new ListBox();
            contextMenu = new ContextMenuStrip(components);
            deleteToolStripMenuItem = new ToolStripMenuItem();
            filterPanel = new Panel();
            txtFilter = new TextBox();
            lblFilter = new Label();
            lvShortcuts = new ListView();
            imageListLarge = new ImageList(components);
            rtbLog = new RichTextBox();
            tableLayoutPanel1.SuspendLayout();
            ((ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            contextMenu.SuspendLayout();
            filterPanel.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(txtPath, 0, 0);
            tableLayoutPanel1.Controls.Add(splitContainer1, 0, 1);
            tableLayoutPanel1.Controls.Add(rtbLog, 0, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            tableLayoutPanel1.Size = new Size(800, 600);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // txtPath
            // 
            txtPath.Dock = DockStyle.Fill;
            txtPath.Location = new Point(3, 3);
            txtPath.Name = "txtPath";
            txtPath.ReadOnly = true;
            txtPath.Size = new Size(794, 23);
            txtPath.TabIndex = 0;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(3, 32);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(lbShortcuts);
            splitContainer1.Panel1.Controls.Add(filterPanel);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(lvShortcuts);
            splitContainer1.Size = new Size(794, 445);
            splitContainer1.SplitterDistance = 220;
            splitContainer1.TabIndex = 1;
            // 
            // lbShortcuts
            // 
            lbShortcuts.AllowDrop = true;
            lbShortcuts.BorderStyle = BorderStyle.FixedSingle;
            lbShortcuts.ContextMenuStrip = contextMenu;
            lbShortcuts.Dock = DockStyle.Fill;
            lbShortcuts.ItemHeight = 15;
            lbShortcuts.Location = new Point(0, 0);
            lbShortcuts.Name = "lbShortcuts";
            lbShortcuts.Size = new Size(220, 415);
            lbShortcuts.TabIndex = 0;
            lbShortcuts.SelectedIndexChanged += Control_SelectedIndexChanged;
            lbShortcuts.DragDrop += LbShortcuts_DragDrop;
            lbShortcuts.DragEnter += ListControl_DragEnter;
            lbShortcuts.DoubleClick += LbShortcuts_DoubleClick;
            lbShortcuts.KeyDown += LbShortcuts_KeyDown;
            // 
            // contextMenu
            // 
            contextMenu.Items.AddRange(new ToolStripItem[] { deleteToolStripMenuItem });
            contextMenu.Name = "contextMenu";
            contextMenu.Size = new Size(108, 26);
            // 
            // deleteToolStripMenuItem
            // 
            deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            deleteToolStripMenuItem.Size = new Size(107, 22);
            deleteToolStripMenuItem.Text = "Delete";
            deleteToolStripMenuItem.Click += DeleteToolStripMenuItem_Click;
            // 
            // filterPanel
            // 
            filterPanel.Controls.Add(txtFilter);
            filterPanel.Controls.Add(lblFilter);
            filterPanel.Dock = DockStyle.Bottom;
            filterPanel.Location = new Point(0, 415);
            filterPanel.Name = "filterPanel";
            filterPanel.Padding = new Padding(0, 2, 0, 0);
            filterPanel.Size = new Size(220, 30);
            filterPanel.TabIndex = 1;
            // 
            // txtFilter
            // 
            txtFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtFilter.Location = new Point(52, 6);
            txtFilter.Name = "txtFilter";
            txtFilter.Size = new Size(160, 23);
            txtFilter.TabIndex = 1;
            txtFilter.TextChanged += txtFilter_TextChanged;
            // 
            // lblFilter
            // 
            lblFilter.Anchor = AnchorStyles.Left;
            lblFilter.AutoSize = true;
            lblFilter.Location = new Point(8, 9);
            lblFilter.Name = "lblFilter";
            lblFilter.Size = new Size(36, 15);
            lblFilter.TabIndex = 0;
            lblFilter.Text = "Filter:";
            lblFilter.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lvShortcuts
            // 
            lvShortcuts.AllowDrop = true;
            lvShortcuts.BorderStyle = BorderStyle.FixedSingle;
            lvShortcuts.ContextMenuStrip = contextMenu;
            lvShortcuts.Dock = DockStyle.Fill;
            lvShortcuts.LargeImageList = imageListLarge;
            lvShortcuts.Location = new Point(0, 0);
            lvShortcuts.Name = "lvShortcuts";
            lvShortcuts.Size = new Size(570, 445);
            lvShortcuts.TabIndex = 0;
            lvShortcuts.UseCompatibleStateImageBehavior = false;
            lvShortcuts.ItemSelectionChanged += LvShortcuts_ItemSelectionChanged;
            lvShortcuts.DragDrop += LvShortcuts_DragDrop;
            lvShortcuts.DragEnter += ListControl_DragEnter;
            lvShortcuts.KeyDown += LvShortcuts_KeyDown;
            lvShortcuts.MouseDoubleClick += LvShortcuts_MouseDoubleClick;
            // 
            // imageListLarge
            // 
            imageListLarge.ColorDepth = ColorDepth.Depth32Bit;
            imageListLarge.ImageSize = new Size(48, 48);
            imageListLarge.TransparentColor = Color.Transparent;
            // 
            // rtbLog
            // 
            rtbLog.Dock = DockStyle.Fill;
            rtbLog.Location = new Point(3, 483);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(794, 114);
            rtbLog.TabIndex = 2;
            rtbLog.Text = "";
            // 
            // ShortcutShelfForm
            // 
            ClientSize = new Size(800, 600);
            Controls.Add(tableLayoutPanel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MinimumSize = new Size(600, 400);
            Name = "ShortcutShelfForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ShortcutShelf";
            FormClosing += ShortcutShelfForm_FormClosing;
            Load += ShortcutShelfForm_Load;
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            contextMenu.ResumeLayout(false);
            filterPanel.ResumeLayout(false);
            filterPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
    }
}
