//
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

//
//
namespace gpp.builder;


//#nullable disable




internal sealed class GrammarTables
{
  private GrammarTables.GrammarIdentifierList m_UsedSetNames      = new GrammarTables.GrammarIdentifierList();
  private GrammarTables.GrammarSetList        m_UserSets          = new GrammarTables.GrammarSetList();
  private GrammarTables.GrammarGroupsList     m_Groups            = new GrammarTables.GrammarGroupsList();
  private GrammarTables.GrammarSymbolList     m_TerminalDefs      = new GrammarTables.GrammarSymbolList();
  private GrammarTables.GrammarSymbolList     m_HandleSymbols     = new GrammarTables.GrammarSymbolList();
  private GrammarTables.GrammarProductionList m_Productions       = new GrammarTables.GrammarProductionList();
  private GrammarAttributesList               m_SymbolAttributes  = new GrammarAttributesList();
  private GrammarAttributesList               m_GroupAttributes   = new GrammarAttributesList();
  private GrammarPropertiesList               m_Properties        = new();

  internal GrammarTables.GrammarIdentifierList UsedSetNames      => m_UsedSetNames;
  internal GrammarTables.GrammarSetList        UserSets          => m_UserSets;
  internal GrammarPropertiesList               Properties        => m_Properties;
  internal GrammarTables.GrammarGroupsList     Groups            => m_Groups;
  internal GrammarTables.GrammarSymbolList     TerminalDefs      => m_TerminalDefs;
  internal GrammarTables.GrammarSymbolList     HandleSymbols     => m_HandleSymbols;
  internal GrammarTables.GrammarProductionList Productions       => m_Productions;
  internal GrammarAttributesList               SymbolAttributes  => m_SymbolAttributes;
  internal GrammarAttributesList               GroupAttributes   => m_GroupAttributes;
  //
  //
  private AppLog                  m_log;
  private PreDefinedCharsetsList  m_BuilderPreDefinedCharacterSets;
  public GrammarTables(AppLog log_, PreDefinedCharsetsList builder_predefined_character_sets_)
  {
    m_log = log_;
    m_BuilderPreDefinedCharacterSets = builder_predefined_character_sets_;
  }


  internal bool AddHandleSymbol(GrammarTables.GrammarSymbol Sym)
  {
    if (!this.HandleSymbols.Contains(Sym)) 
    {
      this.HandleSymbols.Add(Sym);
      return true;
    }      
    return false;
  }

  internal bool AddTerminalHead(GrammarTables.GrammarSymbol Sym)
  {
    if (!this.TerminalDefs.Contains(Sym))
    {
      this.TerminalDefs.Add(Sym);
      return true;
    }

    m_log.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Duplicate definition for the terminal '" + Sym.Name + "'", "", Sym.Line.ToString());
    return false;
  }

  internal void AddGroupOrUpdateExisting(GrammarTables.GrammarGroup new_group_)
  {
    //TODO  Какая то странная логика и в конструкторе Группы и здесь при добавлении. 
    //      Видится, что при дублировании лучше Ош бросать - надежнее будет
    GrammarGroup? existing_group_with_same_name = this.Groups.FindByName(new_group_.Name);
    if (existing_group_with_same_name == null)
      this.Groups.Add(new_group_);
    else 
    {
      if (existing_group_with_same_name.Start.Empty() && !new_group_.Start.Empty())
        existing_group_with_same_name.Start = new_group_.Start;
      else if (existing_group_with_same_name.End.EmptyOrNull() && !new_group_.End.Empty())
        existing_group_with_same_name.End = new_group_.End;
      else
        m_log.Add(AppLogSection.Grammar, AppLogAlert.Warning, "Duplicate group assignment", " The attributes for the group '" + new_group_.Name + " were reassigned.", new_group_.Line.ToString());
    }
  }


  //TODO  Поменять на TryAdd
  internal void AddSymbolAttrib(GrammarAttribute symbol_attr_)
  {
    if ( !this.SymbolAttributes.Contains(symbol_attr_))
      this.SymbolAttributes.Add(symbol_attr_);
    else
      m_log.Add(AppLogSection.Grammar, AppLogAlert.Critical, 
        "Duplicate symbol attribute assignment", "The attributes for the symbol '" + symbol_attr_.Name + "' were set previously.", symbol_attr_.Line.ToString());
  }

  internal void AddGroupAttrib(GrammarAttribute group_attr_)
  {
    if ( !this.GroupAttributes.Contains(group_attr_))
      this.GroupAttributes.Add(group_attr_);
    else
      m_log.Add(AppLogSection.Grammar, AppLogAlert.Critical, 
        "Duplicate group attribute assignment", "The attributes for the group '" + group_attr_.Name + "' were set previously.", group_attr_.Line.ToString());
  }

  internal void AddUserSet(GrammarTables.GrammarSet CharSet)
  {
    if (m_BuilderPreDefinedCharacterSets.Contains(in CharSet.Identificator))
      m_log.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Predined Set", "The set {" + CharSet.Name + "} is a set built into GOLD.", CharSet.Line.ToString());
    else if (this.UserSets.Contains(in CharSet.Identificator))
      m_log.Add(AppLogSection.Grammar, AppLogAlert.Warning, "Set redefined", "The set {" + CharSet.Name + "} was redefined", CharSet.Line.ToString());
    else if (!this.UserSets.Contains(in CharSet.Identificator))
      this.UserSets.Add(CharSet);
    else
      m_log.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Duplicate set definition", "The set '" + CharSet.Name + "' was previously defined.", CharSet.Line.ToString());
  }

  internal void AddUsedSetName(string name_, int line_)
  {
    TIdentificator name_id = MakeIdentificator(name_);
    if (!m_BuilderPreDefinedCharacterSets.Contains(in name_id) && !this.UsedSetNames.Contains(in name_id))
      this.UsedSetNames.Add(new GrammarTables.GrammarIdentifier(name_, line_, in name_id));
  }

  internal void AddProperty(string name_, string value_, int line_)
  {
    if (!this.Properties.Contains(name_))
      this.Properties.Add(name_, value_);
    else
      m_log.Add(AppLogSection.Grammar, AppLogAlert.Warning, "Duplicate property assignment", "The property \"" + name_ + "\" was reassigned.", line_.ToString());
  }

  internal void AddProperty(string name_, GrammarValuesList values_list_, int line_)
  {
    //TODO  Здесь надо разобраться - парсер нас вызывает с готовым объектом, а мы его .ToString()
    //      и передаем дальше как строку с разделителями (см. GrammarAttrValuesList.ToString())
    //      а .ToString() еще и виртуальная
    //      зачем все эти сложности?
    this.AddProperty(name_, values_list_.ToString(), line_);
  }

  internal void AddProduction(GrammarTables.GrammarProduction Prod)
  {
    if(!this.Productions.Contains(Prod))
      this.Productions.Add(Prod);
    else
      m_log.Add(AppLogSection.Grammar, AppLogAlert.Warning, "Duplicate production", "The production '" + Prod.Head.Name + "' was redefined.", Prod.Line.ToString());
  }








  //----------------------------------------------------
  // ** Ниже промежуточная объектная модель построителя **
  //
  //TODO  Теоретически все должно быть private или вряд ли - должно быть доступно BuildLR и BuildDFA ??
  //
  //TODO  Многие списки просятся быть шаблонами!
  //TODO  Все объекты нужно унаследовать от GrammarParseItem, который имеет информацию в какой позиции элемент определен
  internal class GrammarParseItem
  {
    public int Line;
    //
    public GrammarParseItem()
    { }
  }


  internal class GrammarValuesList(string first_value_)
  {
    private List<string> m_data         = [first_value_];
    //
    public int Count()                  => m_data.Count;
    public string this[int index_]       { get => m_data[index_].ToString(); }
    public void Add(string value_)      => m_data.Add(value_);
    public void Add(GrammarValuesList other_attr_values_list_) => m_data.AddRange(other_attr_values_list_.m_data);
    //TODO  Вообще все эти переделывания в строку - хренорво
    // здесь действительно override нужно, т.к. парсер обращается к нам как к объекту
    //TODO  может там поправить можно чтобы виртуальщины небыло?
    public override string ToString()   => m_data.ToStringDelimited(" ");
  }


  internal class GrammarAttrItem
  {
    public string               Name;
    public GrammarValuesList    List;
    public bool                 IsSet;
    //
    public GrammarAttrItem(string name_, GrammarValuesList values_list_, bool is_set_)
    {
      this.Name   = name_;
      this.List   = values_list_;
      this.IsSet  = is_set_;
    }
    public GrammarAttrItem(string name_, string value_, bool is_set_)
      : this(name_, new GrammarValuesList(value_), is_set_)
    { }
    //
    public string Value(string values_separator_ = ", ")
    {
      string s_attr_values_list_content;
      if (this.List.Count() == 0)
        s_attr_values_list_content = "";
      else
      {
        s_attr_values_list_content = this.List[0];
        for (int i = 1; i < this.List.Count(); i++)
          s_attr_values_list_content += (values_separator_ + this.List[i].ToString());
      }

      return this.IsSet ? "{" + s_attr_values_list_content + "}" : s_attr_values_list_content;
    }
    public new string ToString() => this.Value();
  }
  //
  internal class GrammarAttrItemsList : List<GrammarAttrItem>
  {
    public GrammarAttrItemsList(GrammarAttrItem item_) : base()
    {
      base.Add(item_);
    }
  }


  internal class GrammarAttribute(string name_, GrammarAttrItemsList values_list_, int line_)
  {
    public string                   Name    = name_;
    public GrammarAttrItemsList     Values  = values_list_;
    public int                      Line    = line_;
    //
    public readonly TIdentificator  Identificator = MakeIdentificator(name_);
  }


  internal class GrammarAttributesList
  {
    //TODO  в оригинале здесь сравнение было непосредственно по имени безо всякого UCase
    //      делаю также
    private class GrammarAttribAssignEqComparer : IEqualityComparer<GrammarAttribute>
    {
      public bool Equals(GrammarAttribute? x, GrammarAttribute? y)    => x!.Name.Equals(y!.Name);    
      public int GetHashCode([DisallowNull] GrammarAttribute obj)     => obj.Name.GetHashCode();
      //
      public static GrammarAttribAssignEqComparer Instance_           = new GrammarAttribAssignEqComparer();
    }
    //
    private HashSet<GrammarAttribute> m_data;
    //
    public GrammarAttributesList()
    { 
      m_data = new HashSet<GrammarAttribute>(GrammarAttribAssignEqComparer.Instance_);
    }
    //
    public void Add(GrammarAttribute item_)                      => m_data.Add(item_);
    public HashSet<GrammarAttribute>.Enumerator GetEnumerator()  => m_data.GetEnumerator();
    //TODO  Нужно проанализировать алгоритм вызовов и создания объектов GrammarAttribAssign
    //      Сейчас объекты сначала создаются, а только потом проверяются нет ли такого уже в коллекции
    //      - нужно наоборот - сначала проверять, а потом уже создавать если надо (а если дубль - ОШ)
    public bool Contains(GrammarAttribute item_)                 => m_data.Contains(item_);  
  }


  internal class GrammarProperty
  {
    public readonly string          Name;
    protected string                m_Value;
    public string                   Value => m_Value;
    //
    public readonly TIdentificator  Identificator;
    //
    protected GrammarProperty(string name_, string value_)
    {
      this.Name           = name_;
      this.m_Value        = value_;
      this.Identificator  = MakeIdentificator(name_);
    }
  }
  //
  internal class GrammarPropertiesList()
  {
    private class GrammarPropertyCreator(string name_, string value_) : GrammarProperty(name_, value_)
    { }
    //
    private readonly List<GrammarProperty> m_data = new();
    //
    public void Add(string name_, string value_)            => m_data.Add(new GrammarPropertyCreator(name_, value_));
    public List<GrammarProperty>.Enumerator GetEnumerator() => m_data.GetEnumerator();
    public bool Contains(string name_)
    {
      TIdentificator id = MakeIdentificator(name_);
      for (int index = 0; index < m_data.Count; ++index)
        if (m_data[index].Identificator.IsEqual(in id))
          return true;
      return false;
    }  
  }


  internal class GrammarSymbol : GrammarTables.GrammarParseItem
  {
    public readonly SymbolType              Type;
    public readonly string                  Name;
    public readonly TIdentificator          Identificator;
    public readonly TerminalExpression?     TerminalExpression;    
    //
    public GrammarSymbol(string name_, SymbolType type_, int line_, TerminalExpression terminal_expression_)
    {
      this.Name                 = name_;
      this.Identificator        = MakeIdentificator(name_);
      this.Type                 = type_;
      this.Line                 = line_;      
      //
      this.TerminalExpression   = terminal_expression_;      
    }
    public GrammarSymbol(string name_, SymbolType type_, int line_)
    {
      this.Name                 = name_;
      this.Identificator        = MakeIdentificator(name_);
      this.Type                 = type_;
      this.Line                 = line_;

      this.TerminalExpression   = null;
    }

    //
    internal bool IsIdentical(GrammarTables.GrammarSymbol other_)
    {
      if(ReferenceEquals(this, other_)) 
        return true;
      //TODO  Почему то в оригинале в сравнение обязательно включается и тип. Делаю также, но нужно разобраться - теоретически имя должно быть уникальным
      //      (ПС. Не только здесь, но и в GrammarSymbolList)      
      if (Identificator.IsEqual(in other_.Identificator))
      { 
        //Debug.Assert(this.Type == other_.Type);
        if (this.Type == other_.Type)
          return true;
        else
          return false;
      }
      else
        return false;
    }
  }



  internal class GrammarSymbolList
  {
    private List<GrammarSymbol> m_data = new List<GrammarSymbol>();
    //
    public GrammarSymbolList()
    { }
    //
    public int Count                                        => m_data.Count;
    public GrammarSymbol this[int Index]                    => m_data[Index];
    public List<GrammarSymbol>.Enumerator GetEnumerator()   => m_data.GetEnumerator();
    public void Add(GrammarTables.GrammarSymbol item_)      => m_data.Add(item_);
    internal bool Contains(GrammarTables.GrammarSymbol some_sym_) 
    {
      for (int i = 0; i < this.Count; ++i)
      {
        GrammarSymbol my_sym = (GrammarSymbol)m_data[i];
        if (my_sym.IsIdentical(some_sym_))
          return true;
      }
      return false;
    }
    internal bool IsIdentical(GrammarSymbolList other_list_)
    {
      if(ReferenceEquals(this, other_list_)) 
        return true;
      int this_count = this.Count;
      if (this_count != other_list_.Count)
        return false;
      for (int i = 0; i < this_count; ++i)
        if (!this[i].IsIdentical(other_list_[i]))
          return false;

      return true;
    }
  }


  internal class GrammarProduction
  {
    public GrammarTables.GrammarSymbol      Head;
    public GrammarTables.GrammarSymbolList  Handle;
    public int                              Line;
    //
    public GrammarProduction(GrammarTables.GrammarSymbolList handle_symbols_, int line_)
    {
      Head    = default(GrammarTables.GrammarSymbol);
      Handle  = handle_symbols_;
      Line    = line_;
    }
    //
    internal bool IsIdentical(GrammarTables.GrammarProduction other_prod_)
    {
      if (ReferenceEquals(this, other_prod_))
        return true;

      if (!this.Head.IsIdentical(other_prod_.Head))
        return false;

      return this.Handle.IsIdentical(other_prod_.Handle);
    }
  }


  internal class GrammarProductionList
  {
    private List<GrammarProduction> m_data = new List<GrammarProduction>();
    //
    public GrammarProductionList()
    { }
    public GrammarProductionList(GrammarProduction production_)
    { 
      m_data.Add(production_);
    }
    public void Add(GrammarTables.GrammarProduction item_)        => m_data.Add(item_);
    internal size_t Count()                                       => m_data.Count();
    public List<GrammarProduction>.Enumerator GetEnumerator()     => m_data.GetEnumerator();
    internal bool Contains(GrammarTables.GrammarProduction other_prod_)
    {
      for (int i = 0; i < m_data.Count; ++i)
        if (m_data[i].IsIdentical(other_prod_))
          return true;

      return false;
    }
  }


  internal class GrammarGroup : GrammarTables.GrammarParseItem
  {
    public string           Name;
    public string           Container;
    public bool             IsBlock;
    public string           Start;
    public string           End;
    //
    public readonly TIdentificator  Identificator;
    //
    public GrammarGroup(string name_, string Usage, string Value, int Line)
    {
      this.Container      = name_;
      this.Line           = Line;

      //TODO  Бяка это. Usage сюда нужно уже готовым давать енумом!
      string usage_upper = Usage.ToUpper();
      if (usage_upper.Equals("START"))
      {
        this.Name     = name_ + " Block";
        this.IsBlock  = true;
        this.Start    = Value;
      }
      else if (usage_upper.Equals("END"))
      {
        this.Name     = name_ + " Block";
        this.IsBlock  = true;
        this.End      = Value;
      }
      else
      {
        //TODO  Здесь в оригинале было во так странно - т.е. если в конце концов Usage не был равен "LINE", то получался GrammarGroup вообще без имени ??? null ???
        //if (Operators.CompareString(usage_upper, "LINE", true) != 0)
        //  return;
        // Попытаюсь отловить эту ситуацию и понять что к чему. на всякий случай даю имя "" чтобы нулл не было, хотя хз.. может оно так и должно ??
        // но тогда мой initialize_for_equality_comparition помрет !!!
        if (!usage_upper.Equals("LINE"))
        {
          Debug.Assert(false);
          this.Name = "";
        }
        else
        {
          this.Name     = name_ + " Line";
          this.IsBlock  = false;
          this.Start    = Value;
        }
      }

      Identificator = MakeIdentificator(this.Name);
    }
  }


  internal class GrammarGroupsList
  {
    private List<GrammarGroup> m_data = new List<GrammarGroup>();
    //
    public GrammarGroupsList()
    { }
    //
    public void Add(GrammarTables.GrammarGroup Item)        => m_data.Add(Item);
    public List<GrammarGroup>.Enumerator GetEnumerator()    => m_data.GetEnumerator();
    //
    public GrammarGroup? FindByName(string group_name_) 
    {
      TIdentificator group_id = MakeIdentificator(group_name_);
      for (int i = 0; i < m_data.Count; i++)
        if (m_data[i].Identificator.IsEqual(ref group_id))
          return m_data[i];
      
      return null;
    }
  }


  internal class GrammarCharRange
  {
    public readonly char_t char_begin;
    public readonly char_t char_final;
    public GrammarCharRange(int range_begin_, int range_final_)
    {
      this.char_begin = range_begin_;
      this.char_final = range_final_;
    }
  }


  internal class GrammarSet : GrammarTables.GrammarParseItem
  {
    public string                   Name;
    public CharsetExpression        Expression;
    //
    public readonly TIdentificator  Identificator;
    //
    public GrammarSet(string name_, CharsetExpression expr_, int line_)
    {
      this.Name = name_;
      this.Expression = expr_;
      base.Line = line_;
      this.Identificator = MakeIdentificator(name_);
    }
  }


  internal class GrammarSetList
  {
    private List<GrammarSet> m_data = new List<GrammarSet>();
    //
    public GrammarSetList()
    { }
    //
    public size_t Count()                                 => m_data.Count;
    public void Add(GrammarTables.GrammarSet Item)        => m_data.Add(Item);
    public List<GrammarSet>.Enumerator GetEnumerator()    => m_data.GetEnumerator();
    //TODO  У вызывающего плохой алгоритм - if !Contains => Add
    //      если переходить на хэш, то нужно AddIfNotContains
    public bool Contains(ref readonly TIdentificator id_)
    {
      for (int i = 0; i < m_data.Count; i++)
        if (m_data[i].Identificator.IsEqual(in id_))
          return true;
      return false;
    }
  }


  internal class GrammarIdentifier : GrammarTables.GrammarParseItem
  {
    public readonly string          Name;
    public readonly TIdentificator  Identificator;
    //
    public GrammarIdentifier(string name_, int line_, ref readonly TIdentificator identificator_)
    {
      Debug.Assert(MakeIdentificator(name_).IsEqual(in identificator_));
      //
      this.Name             = name_;
      base.Line             = line_;
      this.Identificator    = identificator_;
    }
  }


  internal class GrammarIdentifierList
  {
    private class GrammarIdentifierEqComparer : AFX.IEqualityComparerAB<GrammarIdentifier, GrammarIdentifier>
    {
      public bool IsEqual(ref readonly GrammarIdentifier a_item_, ref readonly GrammarIdentifier b_item_) 
        => a_item_.Identificator.IsEqualCode(in b_item_.Identificator);
      public static readonly GrammarIdentifierEqComparer Instance = new();
    }

    private AFX.THashSetSimpleUnsafe<GrammarIdentifier> m_list = new AFX.THashSetSimpleUnsafe<GrammarIdentifier>(GrammarIdentifierEqComparer.Instance);
    
    private class GrammarIdentifierEqComparerWithIdentificator : AFX.IEqualityComparerAB<GrammarIdentifier, TIdentificator>
    {
      public bool IsEqual(ref readonly GrammarIdentifier a_item_, ref readonly TIdentificator b_item_)    
        => a_item_.Identificator.IsEqualCode(in b_item_);
      public static readonly GrammarIdentifierEqComparerWithIdentificator Instance = new();
    }
    //
    //
    public GrammarIdentifierList()
    { }
    public AFX.THashSetSimpleUnsafe<GrammarIdentifier>.Enumerator GetEnumerator() => m_list.GetEnumerator();
    public void Add(GrammarTables.GrammarIdentifier item_)
    {
      ref readonly GrammarIdentifier existing_item = ref m_list.Add(item_.Identificator.Hash, ref item_);
      //TODO  Нужно исключение - дублирование
      //      Здесь добавление "если не Contains"
      //      нужно просто метод поменять на AddIfNotContains
      Debug.Assert(Unsafe.IsNullRef<GrammarIdentifier>(in existing_item));
    }   
    public bool Contains(ref readonly TIdentificator id_)
    {
      return m_list.Contains(id_.Hash, in id_, GrammarIdentifierEqComparerWithIdentificator.Instance);
    }
  }
}
