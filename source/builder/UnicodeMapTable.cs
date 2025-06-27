//
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static gpp.builder.GrammarTables;


internal class UnicodeMapTable
{
  private struct MyMapItem
  {
    public char_t Code;
    public char_t Map;
  }
  //
  private class MyItemEqComparerWithCode : AFX.IEqualityComparerAB<MyMapItem, char_t>
  {
    bool AFX.IEqualityComparerAB<MyMapItem, char_t>.IsEqual(ref readonly MyMapItem a_item_, ref readonly char_t b_item_)  
      => (a_item_.Code == b_item_);
    public static readonly MyItemEqComparerWithCode Instance = new();
  }
  private class MyItemEqComparer : AFX.IEqualityComparerAB<MyMapItem, MyMapItem>
  {
    bool AFX.IEqualityComparerAB<MyMapItem, MyMapItem>.IsEqual(ref readonly MyMapItem a_item_, ref readonly MyMapItem b_item_) 
      => (a_item_.Code == b_item_.Code);
    public static readonly MyItemEqComparer Instance = new();
  }

  //
  private AFX.THashSetSimpleUnsafe<MyMapItem> m_hash;
  //

  //public:
  public UnicodeMapTable(size_t capacity_)
  {
    m_hash = new (capacity_, MyItemEqComparer.Instance);
  }
  public void Add(char_t code_, char_t map_)
  {
    MyMapItem item;
    item.Code = code_;
    item.Map  = map_;

    ref readonly MyMapItem existing_item = ref m_hash.Add(item.Code.GetHashCode(), in item);
    //TODO  То ли ошибку бросать, то ли тихо глотать дубликаты ?
    Debug.Assert(Unsafe.IsNullRef<MyMapItem>(in existing_item));
  }
  public char_t GetMapOf(char_t code_) 
  {
    ref readonly MyMapItem item = ref m_hash.TryGetValueRefUnsafe(code_.GetHashCode(), in code_, MyItemEqComparerWithCode.Instance);
    if (Unsafe.IsNullRef<MyMapItem>(in item))
      return code_;
    else
      return item.Map;
  }
  public bool ContainsCode(char_t code_)  => m_hash.Contains(code_.GetHashCode(), in code_, MyItemEqComparerWithCode.Instance);
}


internal sealed class UnicodeTable
{
  private UnicodeMapTable m_LowerCaseTable;
  private UnicodeMapTable m_UpperCaseTable;
  private UnicodeMapTable m_Win1252Table;
  //
  public UnicodeTable(UnicodeMapTable unicode_lower_table_, UnicodeMapTable unicode_upper_table_, UnicodeMapTable win1252_table_)
  {
    m_LowerCaseTable  = unicode_lower_table_;
    m_UpperCaseTable  = unicode_upper_table_;
    m_Win1252Table    = win1252_table_;
  }
  //
  public bool IsWin1252(char_t CharCode)
  {
    return (CharCode >= 32 && CharCode <= 126) || (CharCode >= 160 && CharCode <= (int)byte.MaxValue) || m_Win1252Table.ContainsCode(CharCode);
  }
  public int ToUpperCase(char_t CharCode)  => m_LowerCaseTable.GetMapOf(CharCode);
  public int ToLowerCase(char_t CharCode)  => m_UpperCaseTable.GetMapOf(CharCode);
  public int ToWin1252(char_t CharCode)    => m_Win1252Table.GetMapOf(CharCode);
}

