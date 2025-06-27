//
using System.Diagnostics;
//
//
namespace gpp.parser;


internal class ParserCharset
{
  protected struct _charrange_type
  {
    public char_t range_begin;
    public char_t range_final;
  }
  protected _charrange_type[] m_charrangs;
  //
  public readonly int         TableIndex;
  //
  protected ParserCharset(table_index_t table_index_, size_t charset_ranges_count_)
  {
    this.m_charrangs  = new _charrange_type[charset_ranges_count_];
    this.TableIndex   = table_index_;
  }
  // Основной метод нужный парсеру
  public bool Contains(int char_code_)
  {
    return (m_charrangs.Length > 0 && charranges_contains(char_code_, m_charrangs, 0, m_charrangs.Length - 1));
  }
  private bool charranges_contains(char_t value_, _charrange_type[] ranges_, int lo_index_, int hi_index_)
  {
    int _compare_range_with_char(_charrange_type range_, char_t ch_code_)
    {
      if (ch_code_ < range_.range_begin)    // если чар меньша начала диапазона, то, диапазон БОЛЬШЕ чем чар
        return 1;
      if (ch_code_ > range_.range_final)    // если чар больше конца диапазона, то диапазон МЕНЬШЕ чем чар
        return -1;
      return 0;                             // иначе чар попал в диапазон - считаем их "равными" для целей .Contains
    }

    int lo = lo_index_;
    int hi = hi_index_; // если будешь переходить на uint не забудь изменить алгоритм (зациклишься!)

    while (lo <= hi)
    {
      int mid = lo + ((hi - lo) >> 1);
      int cmp = _compare_range_with_char(ranges_[mid], value_);

      if (cmp == 0)
        return true;

      if (cmp < 0)
        lo = mid + 1;
      else
        hi = mid - 1;
    }

    return false;
  }
}

internal class ParserCharsetsList(int size_)
{
  protected List<ParserCharset> m_data = new List<ParserCharset>(size_);
  //
  public ParserCharset this[int index_] => m_data[index_];
}
