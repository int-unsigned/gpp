using Afx.Ui;
using gpp.builder;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gpp_builder
{
  public partial class CtlLog : UserControl
  {
    public CtlLog()
    {
      InitializeComponent();
    }
    //
    public void SelectLast()
    {
      if (lvLogList.Items.Count > 0) 
      {
        lvLogList.Items[lvLogList.Items.Count - 1].Selected = true;
        lvLogList.Items[lvLogList.Items.Count - 1].EnsureVisible();
      }
      
    }
    public void ShowLog(gpp.builder.AppLog log_)
    {
      for (int i = 0; i < log_.Count(); ++i)
      {
        if (log_[i].Alert != AppLogAlert.Detail)
          AddLogItem(log_[i]);
      }
    }

    public void AddLogItem(AppLogItem log_item_)
    {
      var it = lvLogList.Items.Add(log_item_.AlertName());
      it.SubItems.Add(log_item_.SectionName());
      it.SubItems.Add(log_item_.Index);
      it.SubItems.Add(log_item_.Title);
      it.Tag = log_item_;
    }
    public void AddLogEx(string type_, string section_, string title_, string? detail_or_null_)
    {
      var it = lvLogList.Items.Add(type_);
      it.SubItems.Add(section_);
      it.SubItems.Add("" /*log_item_.Index*/);
      it.SubItems.Add(title_);
      it.Tag = detail_or_null_;
    }
    private void lvLog_AjustLastWidth(ListView lv_)
    {
      lv_.Columns[lv_.Columns.Count - 1].Width = -2;
    }

    private void CtlLog_Resize(object sender, EventArgs e)
    {
      lvLog_AjustLastWidth(lvLogList);
    }

    private void lvLogList_SelectedIndexChanged(object sender, EventArgs e)
    {
      tbLogDetail.Text = "";
      
      foreach (ListViewItem lv_item in lvLogList.SelectedItems) 
      {
        if(lv_item.Tag == null)
          continue;

        string s_text = "";
        if (lv_item.Tag is AppLogItem)
        {
          AppLogItem log_item = (AppLogItem)lv_item.Tag;
          s_text = log_item.Description;
          if (s_text.Empty())
            s_text = log_item.Title;

          if (log_item.Alert != AppLogAlert.Success)
            s_text = log_item.AlertName() + "! " + s_text;
        }
        else if(lv_item.Tag is string)
          s_text = (string) lv_item.Tag;

        tbLogDetail.AppendTextLine(s_text);
      }

      tbLogDetail.SelectionStart = 0;
      tbLogDetail.ScrollToCaret();
    }
  }
}
