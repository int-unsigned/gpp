namespace gpp_builder
{
  partial class CtlLog
  {
    /// <summary> 
    /// Обязательная переменная конструктора.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Освободить все используемые ресурсы.
    /// </summary>
    /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Код, автоматически созданный конструктором компонентов

    /// <summary> 
    /// Требуемый метод для поддержки конструктора — не изменяйте 
    /// содержимое этого метода с помощью редактора кода.
    /// </summary>
    private void InitializeComponent()
    {
      lvLogList = new ListView();
      colType = new ColumnHeader();
      colSection = new ColumnHeader();
      colLine = new ColumnHeader();
      colTitle = new ColumnHeader();
      tbLogDetail = new TextBox();
      SuspendLayout();
      // 
      // lvLogList
      // 
      lvLogList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      lvLogList.Columns.AddRange(new ColumnHeader[] { colType, colSection, colLine, colTitle });
      lvLogList.FullRowSelect = true;
      lvLogList.GridLines = true;
      lvLogList.Location = new Point(3, 3);
      lvLogList.Name = "lvLogList";
      lvLogList.Size = new Size(719, 104);
      lvLogList.TabIndex = 0;
      lvLogList.UseCompatibleStateImageBehavior = false;
      lvLogList.View = View.Details;
      lvLogList.SelectedIndexChanged += lvLogList_SelectedIndexChanged;
      // 
      // colType
      // 
      colType.Text = "Type";
      colType.Width = 80;
      // 
      // colSection
      // 
      colSection.Text = "Section";
      colSection.Width = 100;
      // 
      // colLine
      // 
      colLine.Text = "Line";
      // 
      // colTitle
      // 
      colTitle.Text = "Title";
      colTitle.Width = 420;
      // 
      // tbLogDetail
      // 
      tbLogDetail.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      tbLogDetail.Font = new Font("Courier New", 9F, FontStyle.Regular, GraphicsUnit.Point, 204);
      tbLogDetail.Location = new Point(3, 113);
      tbLogDetail.Multiline = true;
      tbLogDetail.Name = "tbLogDetail";
      tbLogDetail.ReadOnly = true;
      tbLogDetail.ScrollBars = ScrollBars.Vertical;
      tbLogDetail.Size = new Size(719, 152);
      tbLogDetail.TabIndex = 1;
      // 
      // CtlLog
      // 
      AutoScaleDimensions = new SizeF(8F, 20F);
      AutoScaleMode = AutoScaleMode.Font;
      Controls.Add(tbLogDetail);
      Controls.Add(lvLogList);
      Name = "CtlLog";
      Size = new Size(725, 268);
      Resize += CtlLog_Resize;
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private ListView lvLogList;
    private ColumnHeader colType;
    private ColumnHeader colSection;
    private ColumnHeader colLine;
    private ColumnHeader colTitle;
    private TextBox tbLogDetail;
  }
}
