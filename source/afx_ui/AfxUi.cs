//
global using static Afx.Ui.UiExtensions;

namespace Afx.Ui
{
  internal static class UiExtensions
  {
    public static void AppendTextLine(this TextBox tb_, string text_)
    {
      tb_.AppendText(text_);
      tb_.AppendText(Environment.NewLine);
    }
  }
}
