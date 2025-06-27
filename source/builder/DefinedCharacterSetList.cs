//
//
using AFX;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace gpp.builder;


internal class PreDefinedCharsetsList
{
  private class PreDefinedCharsetsEqComparer : AFX.IEqualityComparerAB<PreDefinedCharset, TIdentificator>
  {
    public bool IsEqual(ref readonly PreDefinedCharset a_item_, ref readonly TIdentificator b_item_)
    {
      return a_item_.Identificator.IsEqualCode(in b_item_);
    }
    public static PreDefinedCharsetsEqComparer Instance = new PreDefinedCharsetsEqComparer();
  }

  private THashSetSimpleUnsafe<PreDefinedCharset>     m_data;
  private readonly PreDefinedCharset        m_Charset_Whitespace;
  private readonly PreDefinedCharset        m_Charset_AllWhitespace;
  protected PreDefinedCharsetsList(THashSetSimpleUnsafe<PreDefinedCharset> data_, PreDefinedCharset charset_Whitespace_, PreDefinedCharset charset_AllWhitespace_)  
  {
    m_data                  = data_;
    m_Charset_Whitespace    = charset_Whitespace_;
    m_Charset_AllWhitespace = charset_AllWhitespace_;
  }
  //
  public PreDefinedCharset Charset_Whitespace     => m_Charset_Whitespace;
  public PreDefinedCharset Charset_AllWhitespace  => m_Charset_AllWhitespace;
  //
  private ref PreDefinedCharset ItemByNameIdRef(ref readonly TIdentificator name_id_)
  {
    return ref m_data.TryGetValueRefUnsafe(name_id_.Hash, in name_id_, PreDefinedCharsetsEqComparer.Instance);
  }
  private ref readonly PreDefinedCharset ItemByNameIdRefReadonly(ref readonly TIdentificator name_id_)
  {
    return ref ItemByNameIdRef(in name_id_);
  }
  private PreDefinedCharset? ItemByNameId(ref readonly TIdentificator name_id_)
  {
    ref PreDefinedCharset item = ref ItemByNameIdRef(in name_id_);
    if (Unsafe.IsNullRef<PreDefinedCharset>(in item))
      return null;
    else
      return item;
  }
  //
  public PreDefinedCharset? ItemByName(string item_name_)
  {    
    TIdentificator name_id = MakeIdentificator(item_name_);
    return ItemByNameId(ref name_id);
  }
  public bool Contains(ref readonly TIdentificator name_id_)
  {
    ref readonly PreDefinedCharset item_ref = ref ItemByNameIdRefReadonly(in name_id_);
    return ( !Unsafe.IsNullRef<PreDefinedCharset>(in item_ref) );
  }
}



internal class UserDefinedCharsetsList 
{
  protected List<UserDefinedCharset> m_data;
  //
  protected UserDefinedCharsetsList(size_t capacity_)
  { 
    m_data = new List<UserDefinedCharset>(capacity_);
  }
  //
  private int ItemIndexByNameId(ref readonly TIdentificator name_id_)
  {
    for (int i = 0; i < m_data.Count; ++i)
    {
      UserDefinedCharset item = this[i];
      if (m_data[i].Identificator.IsEqual(in name_id_))
        return i;
    }
    return -1;
  }
  //
  //TODO  DefinedCharacterSet как бы HashSet-ready, но CalculateUserDefinedCharsetDependacy формирует список из именно наших индексов 
  //TODO  Надо перерабатывать алгоритм для CalculateUserDefinedCharsetDependacy, тогда можно делать имено HashSet
  public UserDefinedCharset this[int index_]                  => m_data[index_];
  public int Count                                            => m_data.Count;
  public List<UserDefinedCharset>.Enumerator GetEnumerator()  => m_data.GetEnumerator();
  //
  public int ItemIndex(string name_)
  {    
    TIdentificator name_id = MakeIdentificator(name_);
    return ItemIndexByNameId(ref name_id);
  }
  public bool Contains(ref readonly TIdentificator name_id_) 
  {
    return (ItemIndexByNameId(in name_id_) >= 0);
  }
  public UserDefinedCharset? ItemByName(string item_name_)
  { 
    int item_index = ItemIndex(item_name_);
    return (item_index == -1) ? null : this[item_index];
  }
}
