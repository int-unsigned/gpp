//
using System.Diagnostics;
using System.Xml.Linq;

//
//
namespace gpp.builder;


//TODO  Основать на BitArray
//TODO  Dependacy - это DependacyMask
internal class CharsetDependacy
{
  private List<int> m_data;
  //
  public CharsetDependacy()
  {
    m_data = new();
  }
  private CharsetDependacy(CharsetDependacy other_)
  { 
    m_data = new List<int>(other_.m_data);
  }
  //
  public void Add(int value_)
  {
    int insertion_cookie = m_data.BinarySearch(value_);
    if (insertion_cookie < 0)
      m_data.Insert(~insertion_cookie, value_);
  }
  public bool Contains(int value_)
  {
    return (m_data.BinarySearch(value_) >= 0);
  }
  public bool UnionWith(CharsetDependacy other_)
  {
    bool b_union_performed = false;
    for (int i = 0; i < other_.m_data.Count; ++i)
    {
      int value = other_.m_data[i];
      int insertion_cookie = m_data.BinarySearch(value);
      if (insertion_cookie < 0)
      {
        m_data.Insert(~insertion_cookie, value);
        b_union_performed = true;
      }
    }
    return b_union_performed;
  }
  public CharsetDependacy CreateCombineDependacyDependacy(UserDefinedCharsetsList charsets_)
  {
    CharsetDependacy dependacy_dependacy = new CharsetDependacy();
    foreach (table_index_t depended_charset_index in m_data)
      dependacy_dependacy.UnionWith(charsets_[depended_charset_index].Dependacy);
    return dependacy_dependacy;
  }
  public bool UnionWithDependacyDependacy(UserDefinedCharsetsList charsets_)
  {
    CharsetDependacy dependacy_dependacy = this.CreateCombineDependacyDependacy(charsets_);
    return this.UnionWith(dependacy_dependacy);
  }
  public static CharsetDependacy MakeUnionMove(CharsetDependacy movable_a_, CharsetDependacy movable_b_)
  {
    CharsetDependacy result = new CharsetDependacy(movable_a_);
    result.UnionWith(movable_b_);
    return result;
  }
}


internal class DefinedCharsetBase : BuilderFACharset
{
  public readonly string         Name;
  public readonly TIdentificator Identificator;
  protected DefinedCharsetBase(string name_, ref readonly TIdentificator identificator_)
    :base()
  {
    //TODO  Здесь в оригинале использовался .Trim(). Возможно это артефакт, а возможно так надо, хотя именно для "DefinedCharacterSet" это как минимум странно
    //      НО! может быть нюанс в том, что MakeIdentificator конструирует .Code БЕЗ .Trim() и если нас будут искать по независимо сформированному Id из имени с пробелами
    //          то неожиданно могут ненайти..
    //      Оч. желательно иметь единую политику формирования Имени/Ид - или везде .Trim() или нигде.
    Name          = name_; //.Trim();
    Identificator = identificator_;
  }
}


internal class PreDefinedCharset : DefinedCharsetBase
{
  public readonly string Type;
  public readonly string Comment;
  protected PreDefinedCharset(ref readonly TIdentificator identificator_, string name_, string type_, string comment_)
    :base(name_, in identificator_)
  { 
    Comment = comment_;
    Type    = type_;
  }
  //
  //TODO  это плохой костыль
  public static PreDefinedCharset CreateDummyCharset()
  {
    string dummy_name = string.Empty;
    TIdentificator dummy_ident = MakeIdentificator(dummy_name);
    return new PreDefinedCharset(ref dummy_ident, dummy_name, "", "");
  }
}


internal class UserDefinedCharset : DefinedCharsetBase
{
  public readonly   CharsetExpression      Expression;
  private           CharsetDependacy       m_Dependacy;
  //
  public UserDefinedCharset(GrammarTables.GrammarSet grammar_charset_) 
    : base(grammar_charset_.Name, in grammar_charset_.Identificator)
  { 
    this.Expression     = grammar_charset_.Expression;
    this.m_Dependacy    = new();
  }
  //
  public CharsetDependacy Dependacy       => m_Dependacy;
  public void SetDependacy(CharsetDependacy dependacy_)
  { 
    m_Dependacy = dependacy_;
  }
}
