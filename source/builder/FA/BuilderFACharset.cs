//
using System.Diagnostics;
using System.Text;

//
//
namespace gpp.builder;



internal struct EnumeratorDataWithTag
{ 
  public int tag;
  public int index;
}



//TODO  Too many magic consts 0xFF, etc..
internal class BuilderFACharset
{
  private class MyCharsetData
  {
    private const uint m_chars_bmp_max_index = AFX.BitMask256.MaxIndex;
    //
    AFX.BitMask256      m_chars_bmp;    
    CharsRangeList      m_chars_set; 
    private int         m_TableIndex = TABLE_INDEX_DEFAULT;
    //
    private uint to_char_index(char_t char_code_)
    {
      Debug.Assert(is_valid_char(char_code_));
      return unchecked((uint)char_code_);
    }
    //
    public MyCharsetData() 
    {
      m_TableIndex  = TABLE_INDEX_DEFAULT;
      //m_chars_bmp - это структура и инициализации не требует (TODO хотя я возможно неправильно понимаю инициализацию структур)
      m_chars_set   = new();
    }
    public MyCharsetData(table_index_t table_index_)
    {
      m_TableIndex  = table_index_;
      //m_chars_bmp - это структура и инициализации не требует (TODO хотя я возможно неправильно понимаю инициализацию структур)
      m_chars_set   = new();
    }
    public MyCharsetData(MyCharsetData other_)
    {
      m_TableIndex  = other_.m_TableIndex;
      m_chars_bmp   = other_.m_chars_bmp.MakeCopy();
      m_chars_set   = new CharsRangeList(other_.m_chars_set);
    }
    //
    public void Add(int char_)
    { 
      uint char_index = to_char_index(char_);
      if (char_index <= 0xFF)
        m_chars_bmp.Add(char_index);
      else
        m_chars_set.Add(char_);
    }
    public bool Contains(int char_value_)
    {
      Debug.Assert(char_value_ >= 0 && char_value_ <= 0xFFFF);
      if (char_value_ <= 0xFF)
        return m_chars_bmp.Get((uint)char_value_);

      return m_chars_set.Contains(char_value_);
    }
    private bool GetItFromBmpFirst(ref int in_out_index_, out int char_code_)
    {
      Debug.Assert(in_out_index_ == -1);
      uint ix = m_chars_bmp.GetBitFirst();
      if (ix == uint.MaxValue)
      {
        char_code_ = default;
        return false;
      }
        
      in_out_index_ = (int)ix;
      char_code_ = (int)ix;
      return true;
    }
    public bool GetEnumerFirst(out EnumeratorDataWithTag out_enumerator_data_, ref char_t out_char_code_)
    {
      int i_index = -1;
      bool b = GetItFromBmpFirst(ref i_index, out out_char_code_);
      if (b) 
      {
        out_enumerator_data_.tag = -1;
        out_enumerator_data_.index = i_index;
        return true;
      }

      return m_chars_set.GetEnumeratorByChar1st(out out_enumerator_data_, ref out_char_code_);
    }
    public bool GetEnumerNext(ref EnumeratorDataWithTag in_out_enumerator_data_, ref char_t out_char_code_)
    {
      if (in_out_enumerator_data_.tag == -1)
      {
        if (in_out_enumerator_data_.index < m_chars_bmp_max_index)
        {
          uint ix = m_chars_bmp.GetBitNext((uint)in_out_enumerator_data_.index);
          if (ix != uint.MaxValue)
          {
            in_out_enumerator_data_.index = (int)ix;
            out_char_code_ = (char_t)ix;
            return true;
          }
        }
        return m_chars_set.GetEnumeratorByChar1st(out in_out_enumerator_data_, ref out_char_code_);
      }
      else
      {
        return m_chars_set.GetEnumeratorByCharNext(ref in_out_enumerator_data_, ref out_char_code_);
      }
    }
    public size_t CalculateCharsCount()
    { 
      return m_chars_bmp.CountBitsSet_1() + m_chars_set.CalculateCharsCount();
    }
    public bool IsEmpty()
    { 
      return m_chars_bmp.IsAllSet_0() && (m_chars_set.Empty());
    }
    public int GetTableIndex() => m_TableIndex;
    public void SetTableIndex(int table_index_) { m_TableIndex = table_index_; }
    public bool IsEqualSet(MyCharsetData other_)
    { 
      return m_chars_bmp.IsEqual(other_.m_chars_bmp) && m_chars_set.IsEqualSet(other_.m_chars_set);
    }
    public void Remove(MyCharsetData other_)
    {
      m_chars_bmp.Remove(ref other_.m_chars_bmp);
      m_chars_set.Remove(other_.m_chars_set);
    }
    public void Remove(params int[] numbers_)
    {
      for (int i = 0; i < numbers_.Length; ++i) 
      { 
        int ch = numbers_[i];
        Debug.Assert(ch >= 0 && ch <= 0xFFFF);
        if (ch < 256)
          m_chars_bmp.Del(unchecked((uint)ch));
        else
          m_chars_set.Remove(ch);
      }
    }
    public void SetUnionWith(MyCharsetData other_)
    {
      m_chars_bmp.SetUnionWith(ref other_.m_chars_bmp);
      m_chars_set.SetUnionWith(other_.m_chars_set);
    }
    public void AssignMove(MyCharsetData other_)
    {
      m_chars_bmp.AssignMove(ref other_.m_chars_bmp);
      m_chars_set.AssignMove(other_.m_chars_set);
    }

    public void AddRange(int char_code_1st_, int char_code_last_)
    {
      Debug.Assert(is_valid_char(char_code_1st_));
      Debug.Assert(is_valid_char(char_code_last_));
      Debug.Assert(is_valid_range(char_code_1st_, char_code_last_));
      if (char_code_last_ < 256)
        m_chars_bmp.AddRange(unchecked((uint)char_code_1st_), unchecked((uint)char_code_last_));
      else if (char_code_1st_ >= 256)
        m_chars_set.AddRange(char_code_1st_, char_code_last_);
      else
      {
        m_chars_bmp.AddRange(unchecked((uint)char_code_1st_), 255);
        m_chars_set.AddRange(256, char_code_last_);
      }
    }
    public void AddSequentionalRangeUnsafe(int char_code_1st_, int char_code_last_)
    {
      Debug.Assert(is_valid_char(char_code_1st_));
      Debug.Assert(is_valid_char(char_code_last_));
      Debug.Assert(is_valid_range(char_code_1st_, char_code_last_));
      if (char_code_last_ < 256)
        m_chars_bmp.AddRange(unchecked((uint)char_code_1st_), unchecked((uint)char_code_last_));
      else if (char_code_1st_ >= 256)
        m_chars_set.AddSequentionalRangeUnsafe(char_code_1st_, char_code_last_);
      else
      {
        m_chars_bmp.AddRange(unchecked((uint)char_code_1st_), 255);
        m_chars_set.AddSequentionalRangeUnsafe(256, char_code_last_);
      }
    }

    private CharsRangeList m_chars_bmp_to_RangeList()
    {
      CharsRangeList number_range_list = new CharsRangeList();
      uint uch_begin  = m_chars_bmp.GetBitFirst();
      Debug.Assert(is_valid_char(uch_begin));
      uint uch_final  = uch_begin;
      if (uch_final < 255)
      {
        uint uch_next = m_chars_bmp.GetBitNext(uch_final);
        if (uch_next != uint.MaxValue)
        {
          do
          {
            if (uch_next == uch_final + 1)
              uch_final = uch_next;
            else
            {
              number_range_list.AddSequentionalRangeUnsafe(to_char(uch_begin), to_char(uch_final));
              uch_begin = uch_next;
              uch_final = uch_next;
            }
            if (uch_final == 255)
              break;
            else
              uch_next = m_chars_bmp.GetBitNext(uch_final);
          } while (uch_next != uint.MaxValue);
        }
      }
      number_range_list.AddSequentionalRangeUnsafe(to_char(uch_begin), to_char(uch_final));
      return number_range_list;
    }

    public CharsRangeList RangeList()
    {
      if (m_chars_bmp.IsAllSet_0())
        return m_chars_set;
      else
        return CharsRangeList.CombineSequentionalMove(m_chars_bmp_to_RangeList(), m_chars_set);
    }
  }
  //
  //
  private MyCharsetData my_data_;
  

  public BuilderFACharset() 
  {
    my_data_ = new MyCharsetData();
  }
  public BuilderFACharset(table_index_t table_index_)
  {
    my_data_ = new MyCharsetData(table_index_);
  }
  public BuilderFACharset(char_t range_begin_, char_t range_final_)
  {
    my_data_ = new MyCharsetData();
    my_data_.AddRange(range_begin_, range_final_);
  }
  public BuilderFACharset(BuilderFACharset other_charset_)
  {
    my_data_ = new MyCharsetData(other_charset_.my_data_);
  }
  public BuilderFACharset(string charset_string_)
  {
    my_data_ = new MyCharsetData();

    for (int i = 0; i < charset_string_.Length; ++i)
      my_data_.Add(charset_string_[i]);
  }
  public BuilderFACharset(char charcode_)
  {
    my_data_ = new MyCharsetData();
    my_data_.Add(charcode_);
  }
  //
  protected void AddRangeFromLoader(int ch_1st_, int ch_last_) => my_data_.AddSequentionalRangeUnsafe(ch_1st_, ch_last_);


  public struct CharacterSetBuildEnumerator
  {
    private readonly BuilderFACharset   m_charset;
    private EnumeratorDataWithTag       m_enumer;
    private char_t                      m_current_char;
    private bool                        m_bof;
    private bool                        m_eof;
    //
    internal CharacterSetBuildEnumerator(BuilderFACharset charset_)
    {
      m_charset       = charset_;
      m_bof           = true;
      m_eof           = false;
    }
    public bool MoveNext()
    {
      if (m_eof) 
        return false;

      bool b;
      if (m_bof)
      {
        b = m_charset.my_data_.GetEnumerFirst(out m_enumer, ref m_current_char);
        m_bof = false;
      }
      else 
        b = m_charset.my_data_.GetEnumerNext(ref m_enumer, ref m_current_char);

      if (!b)
        m_eof = true;

      return b;
    }
    public readonly char_t Current => m_current_char;
  }
  //
  public CharacterSetBuildEnumerator GetEnumerator()                => new CharacterSetBuildEnumerator(this);


  public void Add(int char_value_)                                  => my_data_.Add(char_value_);
  public bool IsEqualSet(BuilderFACharset other_charset_)           => my_data_.IsEqualSet(other_charset_.my_data_);
  public bool Contains(int char_value_)                             => my_data_.Contains(char_value_);
  public void Remove(params int[] Numbers)                          => my_data_.Remove(Numbers);
  public void SetUnionWith(BuilderFACharset other_charset_)         => my_data_.SetUnionWith(other_charset_.my_data_);
  public void SetDifferenceWith(BuilderFACharset other_charset_)    => my_data_.Remove(other_charset_.my_data_); 
  public void AssignMove(BuilderFACharset other_charset_)           => my_data_.AssignMove(other_charset_.my_data_);
  public BuilderFACharset MakeCopy()                                => new BuilderFACharset(this);
  public void AddRange(int StartValue, int LastValue)               => my_data_.AddRange(StartValue, LastValue);
  public CharsRangeList RangeList()                                 => my_data_.RangeList();
  public bool IsEmpty()                                             => my_data_.IsEmpty();
  public int TableIndex                                             => my_data_.GetTableIndex();
  public void SetTableIndex(table_index_t table_index_)
  {
    my_data_.SetTableIndex(table_index_);
  }
  public size_t CalculateCharsCount()                               => my_data_.CalculateCharsCount();



  //TODO  Here union with not intersect sequence. Algorithm may be better.
  public void PerformCaseClosure(UnicodeTable unicode_table_)
  {    
    MyCharsetData SetB = new(); 
    //
    EnumeratorDataWithTag my_enumer;
    char_t                char_code = default;
    if (my_data_.GetEnumerFirst(out my_enumer, ref char_code))
    {
      do
      {
        int char_code_low = unicode_table_.ToLowerCase(char_code);
        if (char_code_low != char_code)
          SetB.Add(char_code_low);
        else
        {
          int char_code_upp = unicode_table_.ToUpperCase(char_code);
          if (char_code_upp != char_code)
            SetB.Add(char_code_upp);
        }
      } while (my_data_.GetEnumerNext(ref my_enumer, ref char_code));
    }

    this.my_data_.SetUnionWith(SetB);
  }
  public void PerformMappingClosure(UnicodeTable unicode_table_)
  {
    MyCharsetData SetB = new();
    //
    EnumeratorDataWithTag my_enumer;
    char_t                char_code = default;
    if (my_data_.GetEnumerFirst(out my_enumer, ref char_code))
    {
      do
      {
        int char_code_win1252 = unicode_table_.ToWin1252(char_code);
        if (char_code_win1252 != char_code)
          SetB.Add(char_code_win1252);
      } while (my_data_.GetEnumerNext(ref my_enumer, ref char_code));
    }
    this.my_data_.SetUnionWith(SetB);
  }

} // class CharacterSetBuild
