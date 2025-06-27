//
using System;
using System.Globalization;



//TODO  Need reconstruct this to BuilderSymbol, BuilderSymbolView and so on
#nullable disable


//
//
namespace gpp.builder;


internal enum SymbolCreationType
{
  Defined,
  Generated,
  Implicit,
}


internal class BuilderSymbol 
{
  private   string          m_Name;
  protected SymbolType      m_Type;
  protected table_index_t   m_TableIndex;
  //
  public TerminalExpression?          RegularExp;
  public bool                         VariableLength;
  public bool                         UsesDFA;
  public bool                         Accepted;
  public SymbolCreationType           CreatedBy;
  public bool                         Reclassified;
  public BuilderLRLookaheadSymbolSet  First;
  public bool                         Nullable;
  public BuilderLRConfigSet           PartialClosure;
  public string                       LinkName;
  //
  private BuilderGroup                m_Group;
  //
  public readonly  TIdentificator     Identificator;
  public bool IsIdentical(ref readonly TIdentificator other_)
  {
    return (this.Identificator.IsEqual(in other_));
  }
  //
  public const string                   s_Symbol_NewLine                      = "NewLine";
  public static readonly TIdentificator s_Symbol_NewLine_Ident                = MakeIdentificator(s_Symbol_NewLine);
  public const string                   s_Symbol_Whitespace                   = "Whitespace";
  public static readonly TIdentificator s_Symbol_Whitespace_Ident             = MakeIdentificator(s_Symbol_Whitespace);
  const string                          s_Symbol_Comment                      = "Comment";
  public static readonly TIdentificator s_Symbol_Comment_Ident                = MakeIdentificator(s_Symbol_Comment);
  const string                          s_SYMBOL_EOF                          = "EOF";
  public static readonly TIdentificator s_SYMBOL_EOF_IDENT                    = MakeIdentificator(s_SYMBOL_EOF);
  const string                          s_SYMBOL_Error                        = "Error";
  public static readonly TIdentificator s_SYMBOL_Error_IDENT                  = MakeIdentificator(s_SYMBOL_Error);
  const string                          s_SYMBOL_SPECIAL_NON_TERMINAL         = "S'";
  public static readonly TIdentificator s_SYMBOL_SPECIAL_NON_TERMINAL_IDENT   = MakeIdentificator(s_SYMBOL_SPECIAL_NON_TERMINAL);

  public const table_index_t ii_TABLE_INDEX_FOR_END = 0;
  public const table_index_t ii_TABLE_INDEX_FOR_ERR = 1;
  public static table_index_t TableIndexForConstructedMin => ii_TABLE_INDEX_FOR_ERR + 1;
  //
  //TODO  Что-то очень много всяких разных конструкторов. причесать надо
  protected BuilderSymbol(TIdentificator identificator_, string name_, SymbolType type_, bool UsesDFA_, SymbolCreationType created_by_, table_index_t table_index_, TerminalExpression? regexp_or_null_)
  {
    this.m_Name         = name_;
    this.m_Type         = type_;
    this.m_TableIndex   = table_index_;

    this.First          = new BuilderLRLookaheadSymbolSet();
    this.UsesDFA        = UsesDFA_;
    this.CreatedBy      = created_by_;
    this.Reclassified   = false;
    this.RegularExp     = regexp_or_null_;
    this.Identificator  = identificator_;
    //TODO  Пока не знаю зачем это. Нигде не используется
    this.LinkName       = string.Empty;
  }
  //
  public static BuilderSymbol CreateDefaultSymbol_End()
  {
    return new BuilderSymbol(s_SYMBOL_EOF_IDENT, s_SYMBOL_EOF, SymbolType.End, UsesDFA_: false, SymbolCreationType.Generated, table_index_: ii_TABLE_INDEX_FOR_END, regexp_or_null_: null);
  }
  public static BuilderSymbol CreateDefaultSymbol_Err()
  {
    return new BuilderSymbol(s_SYMBOL_Error_IDENT, s_SYMBOL_Error , SymbolType.Error, UsesDFA_: false, SymbolCreationType.Generated, table_index_: ii_TABLE_INDEX_FOR_ERR, regexp_or_null_: null);
  }

  //TODO  Вот это используется в LRBuilder::CreateInitialState, но в таблицу симболов не добавляется
  public static BuilderSymbol CreateSpecialNonTerminal()
  {    
    return new BuilderSymbol(s_SYMBOL_SPECIAL_NON_TERMINAL_IDENT, s_SYMBOL_SPECIAL_NON_TERMINAL, SymbolType.Nonterminal, UsesDFA_:false, SymbolCreationType.Generated, TABLE_INDEX_DEFAULT, regexp_or_null_: null);
  }
  //
  internal bool IsTheSame(BuilderSymbol other_)
  {
    return Object.ReferenceEquals(this, other_);
  }

  public table_index_t  TableIndex   => m_TableIndex;
  public string         Name         => m_Name;
  public SymbolType     Type         => m_Type;
  



  internal string CreatedByName()
  {
    switch (this.CreatedBy)
    {
      case SymbolCreationType.Defined:      return "Defined in Grammar";
      case SymbolCreationType.Generated:    return "Generated";
      case SymbolCreationType.Implicit:     return "Implicitly Defined";
      default:                              return "?TODO-Unspecified?";
    }
  }


  //TODO  вот это все ImpliedDFAUsage... не нравится!
  private static SymbolType ImpliedDFAUsageType(SymbolType type_)
  {
    switch (type_)
    {
      case SymbolType.Content:
      case SymbolType.Noise:
      case SymbolType.GroupStart:
      case SymbolType.GroupEnd:     return ~SymbolType.Nonterminal;
      default:                      return SymbolType.Nonterminal;
    }
  }
  public static bool ImpliedDFAUsage(SymbolType type_)
  {
    return (BuilderSymbol.ImpliedDFAUsageType(type_) != 0);
  }


  public BuilderGroup Group => m_Group;
  public void SetGroupFromConstructor(BuilderGroup group_)
  { 
    m_Group = group_;
  }
  public void SetGroupFromLoader(BuilderGroup group_)
  {
    m_Group = group_;
  }


  public static int CompareSymbols(BuilderSymbol a_, BuilderSymbol b_)
  {
    short a_kind_value = SymbolKindPriority(a_.Type);
    short b_kind_value = SymbolKindPriority(b_.Type);
    //TODO  этот метод используется в бюлдере для сортировки таблицы симболов (SymbolBuildList::Sort)
    //      ранее, в оригинале, там была примитивная сортировка выбором на опоре IsLessThan
    //      и, вообще то, алгоритмы чучуть отличаются - у IsLessThan если a_kind_value > b_kind_value возвращалось false - "не меньше"
    //      а мы сразу возвращаем 1 - "больше"
    //      Вроде все совпадает на моих тестовых данных, но хз.. может где-то и вылезти..
    if (a_kind_value < b_kind_value)
      return -1;
    else if (a_kind_value > b_kind_value)
      return 1;

    return String.Compare(a_.Name, b_.Name, CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth);
  }

  private static short SymbolKindPriority(SymbolType Type)
  {
    switch (Type)
    {
      case SymbolType.Nonterminal:  return 5;
      case SymbolType.Content:      return 4;
      case SymbolType.Noise:        return 2;
      //TODO  По организации таблицы симболов ВСЕГДА первым должен быть .End, а вторым .Error
      //      Но из правил сортироваки это следует только лишь на основе текстового сравнения "EOF" < "ERROR", что плоховато..
      //      ТАКЖЕ! В списке типов есть еще .LEGACYCommentLine которая (вроде) не используется, НО! получит приоритет 0 и все испортится!
      //TODO  Вероятно самым целесообразным выходом будет сортировать таблицу симболов не с начала, а от 3-го элемента и до конца
      //      не затрагивая первые два фиксированных .End и .Error
      case SymbolType.End:
      case SymbolType.Error:        return 1;
      case SymbolType.GroupStart: 
      case SymbolType.GroupEnd:     return 3;
      default:                      return 0;
    }
  }
  internal SymbolCategory Category()
  {
    switch (this.Type)
    {
      case SymbolType.Nonterminal:  return SymbolCategory.Nonterminal;
      case SymbolType.Content:
      case SymbolType.Noise:
      case SymbolType.GroupStart:
      case SymbolType.GroupEnd:     return SymbolCategory.Terminal;
      case SymbolType.End:
      case SymbolType.Error:        return SymbolCategory.Special;
      default:                      return SymbolCategory.Nonterminal;
    }
  }
  internal bool IsFormalTerminal()
  {
    switch (this.Type)
    {
      case SymbolType.Content:
      case SymbolType.GroupStart:
      case SymbolType.GroupEnd:     return true;
      default:                      return false;
    }
  }




  public string Text(bool AlwaysDelimitTerminals = false)
  {
    string str;
    switch (this.m_Type)
    {
      case SymbolType.Nonterminal:
        str = "<" + this.Name + ">";
        break;
      case SymbolType.End:
      case SymbolType.Error:
        str = "(" + this.Name + ")";
        break;
      default:
        str = BuilderSymbol.LiteralFormat(this.Name, AlwaysDelimitTerminals);
        break;
    }
    return str;
  }

  //TODO  если это используется только здесь то почему публичная?
  public static string LiteralFormat(string source_string_, bool always_delimit_)
  {
    //TODO  странное было форматирование - у кого-то было имя одна кавычка? артефакт?
    if (source_string_ == "'")
      return "''";
    else if (always_delimit_)
      return "'" + source_string_ + "'";
    else
    {
      foreach (char c in source_string_)
      { //TODO  Возможно для строки есть более эффективный механизм проверки что в строке есть <небуквы>
        if (!char.IsLetter(c))
          return "'" + source_string_ + "'";
      }
      return source_string_;
    }
  }



  public string TypeName()
  {
    string str = "";
    switch (this.m_Type)
    {
      case SymbolType.Nonterminal:
        str = "Nonterminal";
        break;
      case SymbolType.Content:
        str = "Content";
        break;
      case SymbolType.Noise:
        str = "Noise";
        break;
      case SymbolType.End:
        str = "End of File";
        break;
      case SymbolType.GroupStart:
        str = "Lexical Group Start";
        break;
      case SymbolType.GroupEnd:
        str = "Lexical Group End";
        break;
      case SymbolType.LEGACYCommentLine:
        str = "Comment Line (LEGACY)";
        break;
      case SymbolType.Error:
        str = "Runtime Error Symbol";
        break;
    }
    return str;
  }

}
