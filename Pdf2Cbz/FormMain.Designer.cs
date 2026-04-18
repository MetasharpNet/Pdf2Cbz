namespace Pdf2Cbz
{
    partial class FormMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblDropZone = new Label();
            listBoxLog = new ListBox();
            progressBar = new ProgressBar();
            SuspendLayout();
            // 
            // lblDropZone
            // 
            lblDropZone.AllowDrop = true;
            lblDropZone.BackColor = Color.FromArgb(45, 45, 48);
            lblDropZone.BorderStyle = BorderStyle.FixedSingle;
            lblDropZone.Dock = DockStyle.Top;
            lblDropZone.Font = new Font("Segoe UI", 14F);
            lblDropZone.ForeColor = Color.FromArgb(180, 180, 180);
            lblDropZone.Location = new Point(0, 0);
            lblDropZone.Name = "lblDropZone";
            lblDropZone.Size = new Size(600, 120);
            lblDropZone.TabIndex = 0;
            lblDropZone.Text = "📁 Drop PDF files or folders here";
            lblDropZone.TextAlign = ContentAlignment.MiddleCenter;
            lblDropZone.DragEnter += LblDropZone_DragEnter;
            lblDropZone.DragDrop += LblDropZone_DragDrop;
            // 
            // listBoxLog
            // 
            listBoxLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listBoxLog.BackColor = Color.FromArgb(30, 30, 30);
            listBoxLog.BorderStyle = BorderStyle.None;
            listBoxLog.Font = new Font("Consolas", 9F);
            listBoxLog.ForeColor = Color.FromArgb(200, 200, 200);
            listBoxLog.FormattingEnabled = true;
            listBoxLog.ItemHeight = 14;
            listBoxLog.Location = new Point(0, 120);
            listBoxLog.Name = "listBoxLog";
            listBoxLog.Size = new Size(600, 268);
            listBoxLog.TabIndex = 1;
            // 
            // progressBar
            // 
            progressBar.Dock = DockStyle.Bottom;
            progressBar.Location = new Point(0, 388);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(600, 23);
            progressBar.TabIndex = 2;
            // 
            // FormMain
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            ClientSize = new Size(600, 411);
            Controls.Add(progressBar);
            Controls.Add(listBoxLog);
            Controls.Add(lblDropZone);
            MinimumSize = new Size(400, 300);
            Name = "FormMain";
            Text = "Pdf2Cbz — by Metasharp";
            ResumeLayout(false);
        }

        #endregion

        private Label lblDropZone;
        private ListBox listBoxLog;
        private ProgressBar progressBar;
    }
}
