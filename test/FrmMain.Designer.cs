namespace gpp_maker
{
    partial class FrmMain
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
      bnClose = new Button();
      button2 = new Button();
      lbTestFiles = new CheckedListBox();
      LogView = new gpp_builder.CtlLog();
      SuspendLayout();
      // 
      // bnClose
      // 
      bnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      bnClose.Location = new Point(1046, 444);
      bnClose.Name = "bnClose";
      bnClose.Size = new Size(94, 29);
      bnClose.TabIndex = 0;
      bnClose.Text = "Закрыть";
      bnClose.UseVisualStyleBackColor = true;
      bnClose.Click += bnClose_Click;
      // 
      // button2
      // 
      button2.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      button2.Location = new Point(867, 444);
      button2.Name = "button2";
      button2.Size = new Size(94, 29);
      button2.TabIndex = 2;
      button2.Text = "button2";
      button2.UseVisualStyleBackColor = true;
      button2.Click += button2_Click;
      // 
      // lbTestFiles
      // 
      lbTestFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
      lbTestFiles.FormattingEnabled = true;
      lbTestFiles.IntegralHeight = false;
      lbTestFiles.Location = new Point(867, 12);
      lbTestFiles.Name = "lbTestFiles";
      lbTestFiles.Size = new Size(273, 426);
      lbTestFiles.TabIndex = 3;
      // 
      // LogView
      // 
      LogView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      LogView.Location = new Point(12, 12);
      LogView.Name = "LogView";
      LogView.Size = new Size(849, 467);
      LogView.TabIndex = 5;
      // 
      // FrmMain
      // 
      AutoScaleDimensions = new SizeF(8F, 20F);
      AutoScaleMode = AutoScaleMode.Font;
      CancelButton = bnClose;
      ClientSize = new Size(1152, 485);
      Controls.Add(LogView);
      Controls.Add(lbTestFiles);
      Controls.Add(button2);
      Controls.Add(bnClose);
      Name = "FrmMain";
      Text = "Form1";
      Load += FrmMain_Load;
      ResumeLayout(false);
    }

    #endregion

    private Button bnClose;
    private Button button2;
    private CheckedListBox lbTestFiles;
    private gpp_builder.CtlLog LogView;
  }
}
