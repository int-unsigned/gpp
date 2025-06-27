//
using System;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;


//
//
namespace gpp.builder;
using static gpp.builder.GrammarTables;


internal static class BuilderTablesConstructor
{
  private class BuilderSymbolCtor : BuilderSymbol
  {
    internal BuilderSymbolCtor(TIdentificator identificator_, string name_, SymbolType type_, bool UsesDFA_, SymbolCreationType created_by_, TerminalExpression? regexp_or_null_)
      : base(identificator_, name_, type_, UsesDFA_, created_by_, TABLE_INDEX_DEFAULT, regexp_or_null_)
    { }
    internal BuilderSymbolCtor(TIdentificator identificator_, string name_, SymbolType type_, bool UsesDFA_, SymbolCreationType created_by_)
      : base(identificator_, name_, type_, UsesDFA_, created_by_, TABLE_INDEX_DEFAULT, regexp_or_null_: null)
    { }

    public static BuilderSymbol CreateSymbolVirtual(TIdentificator identificator_, string virtual_terminal_name_) 
    {
      return new BuilderSymbolCtor(identificator_, virtual_terminal_name_, SymbolType.Content, UsesDFA_: false, SymbolCreationType.Defined);
    }    
    public static BuilderSymbol CreateSymbolNewLine(TerminalExpression regexp_NewLine_)
    { 
      return new BuilderSymbolCtor(BuilderSymbol.s_Symbol_NewLine_Ident, BuilderSymbol.s_Symbol_NewLine, SymbolType.Noise, UsesDFA_: true, SymbolCreationType.Generated, regexp_NewLine_);
    }
    public static BuilderSymbol CreateWhitespace(TerminalExpression regexp_Whitespace_)
    { 
      return new BuilderSymbolCtor(BuilderSymbol.s_Symbol_Whitespace_Ident, BuilderSymbol.s_Symbol_Whitespace, SymbolType.Noise, UsesDFA_: true, SymbolCreationType.Generated, regexp_Whitespace_);
    }
    public static BuilderSymbol CreateProductionHead(GrammarProduction grammar_production_)
    {
      return new BuilderSymbolCtor(grammar_production_.Head.Identificator, grammar_production_.Head.Name, SymbolType.Nonterminal, UsesDFA_: false, SymbolCreationType.Defined);
    }
    public static BuilderSymbol CreateImplicitlyDefinedTerminal(GrammarSymbol grammar_handle_symbol_)
    { //TODO  А почему для имплисит-терминала мы UsesDFA_: true ставим, а никакого "регекспа" не прописываем?
      return new BuilderSymbolCtor(grammar_handle_symbol_.Identificator, grammar_handle_symbol_.Name, SymbolType.Content, UsesDFA_: true, SymbolCreationType.Implicit);
    }    
    public static BuilderSymbol CreateGrammarDefinedTerminal(GrammarTables.GrammarSymbol grammart_terminal_def_)
    {
      return new BuilderSymbolCtor(grammart_terminal_def_.Identificator, grammart_terminal_def_.Name, grammart_terminal_def_.Type, UsesDFA_: true, SymbolCreationType.Defined, grammart_terminal_def_.TerminalExpression);
    }
    //
    public void Reclassify(SymbolType new_symbol_type_) 
    { 
      base.m_Type = new_symbol_type_;
      base.Reclassified = true;
    }
    public void SetTableIndex(short new_table_index_)
    {
      base.m_TableIndex = new_table_index_;
    }     
  }

  private class BuilderSymbolsListCtor(int size_, BuilderSymbol initial_symb_end_, BuilderSymbol initial_symb_err_) : BuilderSymbolsList(size_, initial_symb_end_, initial_symb_err_)
  {
    private class SymbComparer : IComparer<BuilderSymbol>
    {
      public int Compare(BuilderSymbol? x, BuilderSymbol? y)  => BuilderSymbol.CompareSymbols(x, y);
      //
      public static readonly SymbComparer Instance_       = new SymbComparer();
    }
    //
    public void AddSymbolDefined(BuilderSymbol symbol_)
    {
      base._internal_add(symbol_);
    }
    public BuilderSymbol AddSymbolNewLine(BuilderSymbol symbol_)
    {
      Debug.Assert(base.m_Symbol_NewLine == null);
      Debug.Assert(symbol_.IsIdentical(in BuilderSymbol.s_Symbol_NewLine_Ident));
      _internal_add(symbol_);
      base.m_Symbol_NewLine = symbol_;
      return symbol_;
    }
    public void AddSymbolWhitespace(BuilderSymbol symbol_)
    {
      base._internal_add(symbol_);
    }

    public BuilderSymbol AddOrGetFromPopulateGroups_Container(GrammarGroup grammar_group_)
    {
      TIdentificator identificator = MakeIdentificator(grammar_group_.Container);
      int symbol_index = ItemIndex(ref identificator, SymbolType.Content);
      if (symbol_index < 0)
        return _internal_add(new BuilderSymbolCtor(identificator, grammar_group_.Container, SymbolType.Content, UsesDFA_: false, SymbolCreationType.Generated));
      else 
      { 
        BuilderSymbol existing_symbol = m_data[symbol_index];
        Debug.Assert(existing_symbol.UsesDFA == false);
        return existing_symbol;
      }
    }
    public BuilderSymbol AddOrGetFromPopulateGroups_Start(GrammarGroup grammar_group_)
    {
      TIdentificator identificator = MakeIdentificator(grammar_group_.Start);
      int symbol_index = ItemIndex(ref identificator, SymbolType.GroupStart);
      if (symbol_index < 0)
        return _internal_add(new BuilderSymbolCtor(identificator, grammar_group_.Start, SymbolType.GroupStart, UsesDFA_: true, SymbolCreationType.Generated));
      else
      {
        BuilderSymbol existing_symbol = m_data[symbol_index];
        Debug.Assert(existing_symbol.UsesDFA == true);
        return existing_symbol;
      }
    }
    public BuilderSymbol AddOrGetFromPopulateGroups_End(GrammarGroup grammar_group_)
    {
      TIdentificator identificator = MakeIdentificator(grammar_group_.End);
      int symbol_index = ItemIndex(ref identificator, SymbolType.GroupEnd);
      if (symbol_index < 0)
        return _internal_add(new BuilderSymbolCtor(identificator, grammar_group_.End, SymbolType.GroupEnd, UsesDFA_: true, SymbolCreationType.Generated));
      else
      {
        BuilderSymbol existing_symbol = m_data[symbol_index];
        Debug.Assert(existing_symbol.UsesDFA == true);
        return existing_symbol;
      }
    }
    public BuilderSymbol AddUniqueFromPopulateProductionHeads(GrammarProduction grammar_production_)
    {
      //TODO  Мы здесь обязательно должны искать именно не-терминалы 
      //      (продукция это конструкция <production> ::= ... )
      //      так как имена терминалов (особенно имплисит-терминалов)
      //      могут совпадать с именами НЕ-терминалов
      int symbol_index = ItemIndex(in grammar_production_.Head.Identificator, SymbolType.Nonterminal);
      if (symbol_index < 0) 
      {
        return base._internal_add(BuilderSymbolCtor.CreateProductionHead(grammar_production_)/* new BuilderSymbol(grammar_production_.Head.Name, SymbolType.Nonterminal)*/);
      }        
      else
      {
        BuilderSymbol existing_symbol = this[symbol_index];
        Debug.Assert(existing_symbol.UsesDFA== false);
        Debug.Assert(existing_symbol.RegularExp == null);
        return existing_symbol;
      }
    }
    public BuilderSymbol AddSymbolHandleImplicitlyDefinedTerminal(GrammarSymbol grammar_handle_symbol_)
    {
      Debug.Assert(grammar_handle_symbol_.Type == SymbolType.Content);      
      BuilderSymbol symbol = BuilderSymbolCtor.CreateImplicitlyDefinedTerminal(grammar_handle_symbol_);
      return base._internal_add(symbol);
    }
    public (bool, BuilderSymbol) AddUniqueFromApplyVirtualProperty(string virtual_terminal_name_)
    {
      TIdentificator identificator = MakeIdentificator(virtual_terminal_name_);
      int symbol_index = ItemIndex(ref identificator);
      if(symbol_index >= 0)
        return (false, base.m_data[symbol_index]);
      else
        //TODO  У меня нет конструктора симбола с уже готовым идентификатором
        //return (true, base._internal_add(new BuilderSymbol(virtual_terminal_name_, SymbolType.Content, UsesDFA_: false)));
        return (true, base._internal_add(BuilderSymbolCtor.CreateSymbolVirtual(identificator,virtual_terminal_name_)));
    }
    public void SortSymbolsAndSetTableIndexes()
    {
      //TODO  стараемся соответствовать сортировке в Голд
      //      для этого мы переключили сборку на использование Nls сравнения, вместо ICU по-умолчанию для NET 
      //      см. файл проекта
      Debug.Assert(IsGlobalizationICUMode() == false);
      base.m_data.Sort(SymbComparer.Instance_);
      //
      for (int i = BuilderSymbol.TableIndexForConstructedMin; i < this.Count(); ++i)
      { 
        BuilderSymbolCtor? symbol_constructed = this[i] as BuilderSymbolCtor;
        Debug.Assert(symbol_constructed != null);
        symbol_constructed.SetTableIndex(to_short(i));
      }        
    }
    public BuilderSymbol? GetNonTerminalSymbolForStartSymbol(string name_)
    {
      TIdentificator  identificator = MakeIdentificator(name_);
      table_index_t   index         = ItemIndex(ref identificator, SymbolType.Nonterminal);
      return(index < 0)? null : m_data[index];
    }
    //TODO  Ну оочень большая суета с этими терминал\не-терминал
    //      Ну оочень непрозрачно получается когда имена терминалов и не-терминалов могут совпадать..
    internal BuilderSymbol? GetTerminalSymbolForReclassify(string name_)
    {
      TIdentificator identificator = MakeIdentificator(name_);
      for (int i = 0; i < this.Count(); ++i)
      {
        BuilderSymbol item = this[i];
        if (item.IsIdentical(ref identificator) && item.Type != SymbolType.Nonterminal)
          return item;
      }
      //
      return null;
    }
    
    internal BuilderSymbolCtor? GetTerminalSymbolForReclassify(ref readonly TIdentificator identificator_)
    {
      for (int i = 0; i < this.Count(); ++i)
      {
        BuilderSymbol item = this[i];
        if (item.IsIdentical(in identificator_) && item.Type != SymbolType.Nonterminal)
        {
          if (item is BuilderSymbolCtor)
            return (BuilderSymbolCtor)item;
          else
          {
            Debug.Assert(false);
            return null;
          }
        }          
      }
      //
      return null;
    }
    public BuilderSymbol? Symbol_NewLine()
    {
      //TODO  хотя это по нормальному вызовется один раз, но все равно надо по человечески сделать
      if (m_Symbol_NewLine == null)
      {
        int ix = ItemIndex(in BuilderSymbol.s_Symbol_NewLine_Ident);
        if (ix >= 0)
          m_Symbol_NewLine = this[ix];
      }
      return m_Symbol_NewLine;
    }

    public short ItemIndex(GrammarTables.GrammarSymbol grammar_symbol_, SymbolType type_)
    {
      return ItemIndex(in grammar_symbol_.Identificator, type_);
    }
    internal bool Contains(string name_) => this.ItemIndex(name_) != (short)-1;
    internal bool Contains(TIdentificator identificator_) => (this.ItemIndex(in identificator_) != (short)-1);

    internal short ItemIndex(string name_)
    {
      TIdentificator identificator = TIdentificator.Create(name_);
      return ItemIndex(in identificator);
    }
    internal short ItemIndex(ref readonly TIdentificator identificator_)
    {
      for (int i = 0; i < this.Count(); ++i)
        if (this[i].Identificator.IsEqual(in identificator_))
          return (short)i;

      return -1;
    }
    protected short ItemIndex(ref readonly TIdentificator identificator_, SymbolType type_)
    {
      for (int i = 0; i < this.Count(); ++i)
      {
        BuilderSymbol item = this[i];
        if (item.IsIdentical(in identificator_) && item.Type == type_)
          return (short)i;
      }
      //
      return -1;
    }
    internal short ItemIndexCategory(GrammarTables.GrammarSymbol grammar_symbol_, SymbolCategory сategory_)
    {
      for (int i = 0; i < this.Count(); ++i)
      {
        BuilderSymbol item = this[i];
        if (item.IsIdentical(in grammar_symbol_.Identificator) && item.Category() == сategory_)
          return (short)i;
      }
      //
      return -1;
    }

    //TODO  Возможно метод Reclassify должен быть у таблицы симболов, т.к. там сортировка и т.п. классификация от типа зависит
    public void Reclassify(BuilderSymbolCtor symbol_, SymbolType new_symbol_type_)
    {
      if (symbol_.Type != new_symbol_type_)
        symbol_.Reclassify(new_symbol_type_);
    }
    public void Reclassify(BuilderSymbol symbol_, SymbolType new_symbol_type_)
    {
      if (symbol_.Type != new_symbol_type_)
      {
        if (symbol_ is BuilderSymbolCtor)
          ((BuilderSymbolCtor)symbol_).Reclassify(new_symbol_type_);
        else
        {
          Debug.Assert(false);
        }
      }        
    }

  } // BuilderSymbolCtor

  internal class BuilderGroupCtor(string name_, BuilderSymbol container_, BuilderSymbol start_symb_, BuilderSymbol end_symb_, GroupEndingMode ending_mode_) 
    : BuilderGroup(TABLE_INDEX_DEFAULT, name_, container_, start_symb_, end_symb_, GroupAdvanceMode.Character, ending_mode_) // GroupAdvanceMode.Character - так было в оригинале
  {
    public new void SetTableIndex(int table_index_) => base.SetTableIndex(table_index_);
  }
  private class BuilderGroupsListCtor() : BuilderGroupsList(0)
  {
    public void Add(BuilderGroupCtor item_) => base.m_data.Add(item_);
    public void AssignTableIndexes()
    {
      for (int i = 0; i < base.Count; ++i)
        ((BuilderGroupCtor)base[i]).SetTableIndex(i);      
    }

    //TODO  Дурацкий алгоритм в CheckDuplicateGroupStart поэтому здесь костыль пока.. Это для tmp-списка и к основному отношения не имеет
    public void AddTmp(BuilderGroup item_) => base.m_data.Add(item_);
  }


  public static (bool, BuilderTables, BuilderSymbol?) Construct(AppLog log_, Builder builder_, GrammarTables grammar_tables_) 
  {
    const string s_TRUE   = "TRUE";
    const string s_FALSE  = "FALSE";

    BuilderPropertiesList   builder_properties    = PopulateProperties(grammar_tables_.Properties);
    (BuilderSymbolsListCtor builder_symbols, 
     BuilderGroupsListCtor  builder_groups)       = PopulateSymbolsAndGrops(log_, builder_properties, builder_.PredefinedSets, grammar_tables_);    
    UserDefinedCharsetsList user_defined_charsets = PopulateUserDefinedCharsets(log_, builder_.PredefinedSets, grammar_tables_.UserSets, grammar_tables_.UsedSetNames);
    BuilderProductionsList  builder_productions   = PopulateProductions(grammar_tables_.Productions, builder_symbols);
    //
    PopulateAttributes(log_, grammar_tables_, builder_symbols, builder_groups);
    ApplyVirtualProperty(log_, builder_properties, builder_symbols);
    //
    PopulateEts(log_, builder_symbols, builder_groups, builder_productions);
    PopulateSymbolUsesDfaCharsets(log_, builder_.PredefinedSets, user_defined_charsets, builder_symbols);
    //
    BuilderTables builder_tables                  = new BuilderTables(builder_properties, builder_symbols, builder_groups, builder_productions, builder_.PredefinedUnicodeTable, user_defined_charsets);
    //
    BuilderSymbol? builder_start_symbol           = PopulateStartSymbol(log_, builder_properties, builder_symbols);

    string s_prop_case_sensitive_VALUE = builder_tables.Properties.CaseSensitiveProperty.Value.ToUpperInvariant();
    if (s_prop_case_sensitive_VALUE != s_TRUE && s_prop_case_sensitive_VALUE != s_FALSE)
      log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Invalid Property Value", "The \"Case Sensitive\" parameter must be either True or False.");

    if (builder_tables.Properties.CharMappingMode == CharMappingMode.Invalid)
      log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Invalid Property Value", "The \"Character Mapping\" parameter must be either Unicode, Windows-1252, or ANSI (same as Windows-1252).");

    //TODO  Не обязательный для построителя информационный блок
    int count_of_symbol_content = 0;
    for (int symbol_index = 0; symbol_index < builder_tables.Symbol.Count(); ++symbol_index)
      if (builder_tables.Symbol[symbol_index].Type == SymbolType.Content)
        ++count_of_symbol_content;
    log_.Add(AppLogSection.Grammar, AppLogAlert.Detail, "The grammar contains a total of " + count_of_symbol_content.ToString() + " formal terminals.");

    if (!log_.LoggedCriticalError() && builder_start_symbol != null)
      BuilderTablesConstructor.DoSemanticAnalysis(log_, builder_tables, builder_start_symbol);

    //TODO  Не обязательный для построителя информационный блок
    if (!log_.LoggedCriticalError())
      log_.Add(AppLogSection.Grammar, AppLogAlert.Success, "The grammar was successfully analyzed");

    return (!log_.LoggedCriticalError(), builder_tables, builder_start_symbol);
  }
  
  private static (BuilderSymbolsListCtor, BuilderGroupsListCtor) PopulateSymbolsAndGrops(AppLog log_, BuilderPropertiesList builder_broperties_, PreDefinedCharsetsList builder_predefined_sets_, GrammarTables grammar_tables_)
  {
    //TODO  с count_of_symbol_hint можно поэкспериментировать..
    int                     count_of_symbol_hint  = grammar_tables_.TerminalDefs.Count + grammar_tables_.Productions.Count() + grammar_tables_.HandleSymbols.Count / 2;
    BuilderSymbolsListCtor  out_symbols           = new BuilderSymbolsListCtor(count_of_symbol_hint, BuilderSymbol.CreateDefaultSymbol_End(), BuilderSymbol.CreateDefaultSymbol_Err());
    BuilderGroupsListCtor   out_groups            = new BuilderGroupsListCtor();

    BuilderTablesConstructor.PopulateSymbolsDefinedTerminals(grammar_tables_.TerminalDefs, out_symbols);

    //TODO  что-то тут сложно
    //      здесь мы можем добавить в симболы NewLine и Group-Start Group-End Group-Container из грамматики
    BuilderTablesConstructor.PopulateGroupsAndWhitespace(log_, builder_broperties_.CharsetMode, builder_predefined_sets_, builder_broperties_,
                                            grammar_tables_.Groups, out_symbols, out_groups);

    // здесь мы добавляем в .Symbol non-терминалы из граматики
    BuilderTablesConstructor.PopulateProductionHeads(grammar_tables_.Productions, out_symbols);
    BuilderTablesConstructor.PopulateHandleSymbols(log_, grammar_tables_.HandleSymbols, out_symbols);

    return (out_symbols, out_groups);
  }

  private static void PopulateAttributes(AppLog log_, GrammarTables grammar_tables_, BuilderSymbolsListCtor builder_symbols_, BuilderGroupsList builder_groups_)
  {
    BuilderTablesConstructor.ApplyGroupAttributes(log_, grammar_tables_.GroupAttributes, builder_groups_);
    BuilderTablesConstructor.ApplySymbolAttributes(log_, grammar_tables_.SymbolAttributes, builder_symbols_);
  }

  private static void PopulateEts(AppLog log_,BuilderSymbolsListCtor builder_symbols_, BuilderGroupsListCtor builder_groups_, BuilderProductionsList builder_productions_)
  {     
    BuilderTablesConstructor.CreateImplicitDeclarationsRegExp(builder_symbols_);
    BuilderTablesConstructor.AssignTableIndexes(builder_symbols_, builder_productions_, builder_groups_);
    BuilderTablesConstructor.LinkGroupSymbolsToGroup(builder_groups_);
    BuilderTablesConstructor.AssignSymbolRegexpPriorities(log_, builder_symbols_);
  }


  private static BuilderPropertiesList PopulateProperties(GrammarPropertiesList grammar_properties_)
  {
    BuilderPropertiesList out_builder_properties = new BuilderPropertiesList();

    foreach (GrammarProperty property in grammar_properties_)
      out_builder_properties.AddGrammarDefinedProperty(property);

    //TODO  несколько странное пост-исправление значения свойства прописанного в грамматике
    //      (значение по-умалчанию для свойства "Character Mapping" - "Windows-1252")
    //      нужно разобраться где, как и на что влияет
    if (out_builder_properties.CharacterMappingProperty.Value.ToUpperInvariant().Contains("UNICODE"))
      out_builder_properties.UpdateCharacterMapping("None");

    return out_builder_properties;
  }


  private class UserDefinedCharsetsListCtor(size_t capacity_) : UserDefinedCharsetsList(capacity_)
  {
    public void AddFromConstructor(GrammarTables.GrammarSet grammar_user_charset_) 
    {
      base.m_data.Add(new UserDefinedCharset(grammar_user_charset_));
    } 
  }

  private static void PopulateSymbolUsesDfaCharsets_CharsetItem(AppLog log_, 
                                                                PreDefinedCharsetsList builder_predefined_charsets_, UserDefinedCharsetsList user_defined_charsets_, 
                                                                BuilderSymbolsListCtor builder_symbols_,
                                                                CharsetExpressionItem terminal_expression_item_charset_)
  {
    if (terminal_expression_item_charset_.Type == CharsetExpressionItemType.Name && terminal_expression_item_charset_.Characters == null)
    {
      BuilderFACharset? named_charset = BuilderUtility.GetUserDefinedOrPredefinedCharacterSet(builder_predefined_charsets_, user_defined_charsets_, terminal_expression_item_charset_.Text!);
      if (named_charset != null)
        terminal_expression_item_charset_.AssignEvalutedNamedCharset(named_charset);
      else
        log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Character set is not defined", "The character set {" + terminal_expression_item_charset_.Text + "} was not defined in the grammar.");
    }
  }
  private static void PopulateSymbolUsesDfaCharsets_ExprItem(AppLog log_, 
                                                              PreDefinedCharsetsList builder_predefined_charsets_, UserDefinedCharsetsList user_defined_charsets_, 
                                                              BuilderSymbolsListCtor builder_symbols_,
                                                              TerminalExpressionItem terminal_expression_item_)
  {
    if (terminal_expression_item_.Data is CharsetExpressionItem)
    {
      CharsetExpressionItem terminal_expression_item_charset = (CharsetExpressionItem)terminal_expression_item_.Data;
      PopulateSymbolUsesDfaCharsets_CharsetItem(log_, builder_predefined_charsets_, user_defined_charsets_, builder_symbols_, terminal_expression_item_charset);
    }
    else 
    {
      Debug.Assert(terminal_expression_item_.Data is TerminalExpression);
      TerminalExpression terminal_expression_item_expr = (TerminalExpression)terminal_expression_item_.Data;
      for (int terminal_expression_item_expr_index = 0; terminal_expression_item_expr_index < terminal_expression_item_expr.Count(); ++terminal_expression_item_expr_index)
      {
        TerminalExpressionSequence terminal_expression_sequence = terminal_expression_item_expr[terminal_expression_item_expr_index];
        for (int terminal_expression_sequence_index = 0; terminal_expression_sequence_index < terminal_expression_sequence.Count(); ++terminal_expression_sequence_index) 
        {
          TerminalExpressionItem terminal_expression_sequence_item = terminal_expression_sequence[terminal_expression_sequence_index];
          PopulateSymbolUsesDfaCharsets_ExprItem(log_, builder_predefined_charsets_, user_defined_charsets_, builder_symbols_, terminal_expression_sequence_item);
        }
      }
    }
  }

  private static void PopulateSymbolUsesDfaCharsets(AppLog log_, 
                                                    PreDefinedCharsetsList builder_predefined_charsets_, UserDefinedCharsetsList user_defined_charsets_, 
                                                    BuilderSymbolsListCtor builder_symbols_)
  {
    for (int symbol_index = 0; symbol_index < builder_symbols_.Count(); ++symbol_index)
    {
      BuilderSymbol symbol = builder_symbols_[symbol_index];
      if (symbol.UsesDFA)
      {
        Debug.Assert(symbol.RegularExp != null && symbol.RegularExp.Count() > 0);
        for (int terminal_expression_sequence_index = 0; terminal_expression_sequence_index < symbol.RegularExp.Count(); ++terminal_expression_sequence_index)
        {
          TerminalExpressionSequence terminal_expression_sequence = symbol.RegularExp[terminal_expression_sequence_index];
          for(int terminal_expression_sequence_item_index = 0; terminal_expression_sequence_item_index < terminal_expression_sequence.Count(); ++terminal_expression_sequence_item_index)            
          {
            TerminalExpressionItem terminal_expression_sequence_item = terminal_expression_sequence[terminal_expression_sequence_item_index];
            PopulateSymbolUsesDfaCharsets_ExprItem(log_, builder_predefined_charsets_, user_defined_charsets_, builder_symbols_, terminal_expression_sequence_item);
          }
        }
      }
    }
  }


  private static UserDefinedCharsetsList PopulateUserDefinedCharsets(AppLog log_, PreDefinedCharsetsList builder_predefined_charsets_, 
                                                                        GrammarSetList grammar_user_charsets_, GrammarTables.GrammarIdentifierList grammar_used_set_names_)
  {
    UserDefinedCharsetsListCtor out_builder_user_defined_sets = new UserDefinedCharsetsListCtor(grammar_user_charsets_.Count());

    foreach (GrammarTables.GrammarSet grammar_user_charset in grammar_user_charsets_)
      out_builder_user_defined_sets.AddFromConstructor(grammar_user_charset);

    bool b_all_user_charset_defined = true;

    foreach (GrammarTables.GrammarIdentifier usedSetName in grammar_used_set_names_)
    {
      if (!out_builder_user_defined_sets.Contains(in usedSetName.Identificator))
      {
        log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Undefined Set", "The set {" + usedSetName.Name + "} is used but not defined in the grammar.", usedSetName.Line.ToString());
        b_all_user_charset_defined = false;
      }
    }

    if (b_all_user_charset_defined)
    {
      foreach (UserDefinedCharset user_defined_charset in out_builder_user_defined_sets)
        user_defined_charset.SetDependacy( BuilderTablesConstructor.CalculateUserDefinedCharsetDependacy(log_, out_builder_user_defined_sets, user_defined_charset) );

      //TODO  Этот алгоритм нужен для определения циклических зависимостей в графе чарсетов
      //      каждому чарсету явно прописываются его прямые и все косвенные зависимости
      //      до тех пор, пока не пропишутся все возможные достижимые
      //      (в т.ч. и он сам в случае циклической зависимости)
      //      Когда нам удалось изменить набор зависимостей у какого-то одного чарсета
      //      нам приходится просматривать зависимости для всех остальных чарсетов заново
      //      так как от этого "какого-то одного чарсета" могут зависеть другие чарсеты
      bool b_some_user_defined_charsets_dirty = true;
      while (b_some_user_defined_charsets_dirty)
      { 
        b_some_user_defined_charsets_dirty = false;
        foreach (UserDefinedCharset user_defined_charset in out_builder_user_defined_sets)
          if (user_defined_charset.Dependacy.UnionWithDependacyDependacy(out_builder_user_defined_sets))
            b_some_user_defined_charsets_dirty = true;
      }

      for (int user_defined_charset_index = 0; user_defined_charset_index < out_builder_user_defined_sets.Count; ++user_defined_charset_index)
      {
        if (out_builder_user_defined_sets[user_defined_charset_index].Dependacy.Contains(user_defined_charset_index))
        {
          log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, 
            "Circular Set Definition", 
            "The set {" + out_builder_user_defined_sets[user_defined_charset_index].Name  + "} is defined using itself. This can be either direct or through other sets.");
          b_all_user_charset_defined = false;
        }
      }
    }

    if (b_all_user_charset_defined)
    {
      bool b_user_defined_charsets_dirty2 = true;
      while (b_user_defined_charsets_dirty2)
      {
        b_user_defined_charsets_dirty2 = false;
        foreach (UserDefinedCharset user_defined_charset in out_builder_user_defined_sets)
        {
          BuilderFACharset tmp_evaluated_charset = CharsetExpressionEvaluate(log_, builder_predefined_charsets_, out_builder_user_defined_sets, user_defined_charset.Expression);
          if (!user_defined_charset.IsEqualSet(tmp_evaluated_charset))
          {
            user_defined_charset.AssignMove(tmp_evaluated_charset);
            b_user_defined_charsets_dirty2 = true;
          }
        }
      }
    }

    return out_builder_user_defined_sets;
  }


  //TODO  Метод возвращает set ИНДЕКСОВ из набора .UserDefinedSets, НЕ САМИХ элементов
  internal static CharsetDependacy CalculateUserDefinedCharsetDependacy(AppLog log_, UserDefinedCharsetsList user_defined_charsets_, UserDefinedCharset user_charset_)
  {
    return CalculateUserDefinedCharsetDependacyExpr(log_, user_defined_charsets_, user_charset_.Expression);
  }
  private static CharsetDependacy CalculateUserDefinedCharsetDependacyExpr(AppLog log_, UserDefinedCharsetsList user_defined_charsets_, CharsetExpression user_charset_expression_)
  {
    CharsetDependacy result_dependacy = new();

    if (user_charset_expression_ is CharsetExpressionItem)
    {
      CharsetExpressionItem set_expression_item = (CharsetExpressionItem) user_charset_expression_;
      if (set_expression_item.Type == CharsetExpressionItemType.Name)
      {
        int user_defined_set_index = user_defined_charsets_.ItemIndex(set_expression_item.Text);
        if (user_defined_set_index != -1)
          result_dependacy.Add(user_defined_set_index);
      }
    }
    else if (user_charset_expression_ is CharsetExpressionOpBinary)
    {
      CharsetExpressionOpBinary set_expression_expr = (CharsetExpressionOpBinary) user_charset_expression_;
      CharsetDependacy SetA = BuilderTablesConstructor.CalculateUserDefinedCharsetDependacyExpr(log_, user_defined_charsets_, set_expression_expr.LeftOperand);
      CharsetDependacy SetB = BuilderTablesConstructor.CalculateUserDefinedCharsetDependacyExpr(log_, user_defined_charsets_, set_expression_expr.RightOperand);
      result_dependacy = CharsetDependacy.MakeUnionMove(SetA, SetB);
    }
    else
      log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Internal Error: Invalid object in set expression.", "Embedded object: " + user_charset_expression_.GetType().FullName);

    return result_dependacy;
  }



  private static BuilderFACharset CharsetExpressionEvaluate(AppLog log_, PreDefinedCharsetsList predefined_charsets_, UserDefinedCharsetsList user_defined_charsets_, CharsetExpression charset_expression_)
  {
    BuilderFACharset? result_charset = null;

    if (charset_expression_ is CharsetExpressionItem)
    {
      CharsetExpressionItem charset_expression_item = (CharsetExpressionItem) charset_expression_;
      switch (charset_expression_item.Type)
      {
        case CharsetExpressionItemType.Chars:
        {
          result_charset = charset_expression_item.Characters;
        }          
        break;
        case CharsetExpressionItemType.Name:
        {
          if (charset_expression_item.Characters != null)
            result_charset = new BuilderFACharset(charset_expression_item.Characters);
          else
          {
            BuilderFACharset? named_charset = BuilderUtility.GetUserDefinedOrPredefinedCharacterSet(predefined_charsets_, user_defined_charsets_, charset_expression_item.Text ! );
            if (named_charset == null)
            {
              log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Character set is not defined", "The character set {" + charset_expression_item.Text + "} was not defined in the grammar.");
              result_charset = new BuilderFACharset();
              charset_expression_item.AssignEvalutedNamedCharset(result_charset);
            }
            else
            {
              //TODO  Мы НЕ ДОЛЖНЫ возвращать ссылку на какие будь ни было "predefined_" или "user_defined_" charsets_
              //      ТОЛЬКО копии!
              //      Так как при всяких дальнейших "Evaluate" к этим "хх-defined_" чарсетам будет что-то добавляться\вычитаться
              //      и получится мрак.
              //      А проблема в том, что "хх-defined_" чарсеты это НЕ КОНСТАНТНЫЕ объекты
              //      Была бы у c# константность не пришлось бы эту портянку писать..
              result_charset = new BuilderFACharset(named_charset);
              //TODO  А вот запомнить мы должны именно сам named_charset, а не его копию. хз.. почему.. непонятно (иначе неработает :()
              charset_expression_item.AssignEvalutedNamedCharset(named_charset);                
            }              
          }
        }
        break;
      }
    }
    else if (charset_expression_ is CharsetExpressionOpBinary)
    {
      CharsetExpressionOpBinary set_expression_expr   = (CharsetExpressionOpBinary) charset_expression_;
      BuilderFACharset          charset_lvalue        = CharsetExpressionEvaluate(log_, predefined_charsets_, user_defined_charsets_, set_expression_expr.LeftOperand);
      BuilderFACharset          charset_rvalue        = CharsetExpressionEvaluate(log_, predefined_charsets_, user_defined_charsets_, set_expression_expr.RightOperand);
      switch (set_expression_expr.Operator)
      {
        case CharsetExpressionOp.Append:
          charset_lvalue.SetUnionWith(charset_rvalue);
          result_charset = charset_lvalue;
          break;
        case CharsetExpressionOp.Remove:
          charset_lvalue.SetDifferenceWith(charset_rvalue);
          result_charset = charset_lvalue;
          break;
        default:
          log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Internal Error: Invalid set operator.", "Operator: '" + set_expression_expr.Operator.ToString() + "'");
          result_charset = (BuilderFACharset) new BuilderFACharset();
          break;
      }
    }
    else
    {
      log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Internal Error: Invalid object in set expression.", "Embedded object: " + charset_expression_.GetType().FullName);
      result_charset = new BuilderFACharset();
    }

    return result_charset;
  }


  private static void PopulateSymbolsDefinedTerminals(GrammarTables.GrammarSymbolList grammar_terminal_defs_, BuilderSymbolsListCtor out_builder_symbols_)
  {
    foreach (GrammarTables.GrammarSymbol terminal_def in grammar_terminal_defs_)
    {
      BuilderSymbol symb = BuilderSymbolCtor.CreateGrammarDefinedTerminal(terminal_def);
      out_builder_symbols_.AddSymbolDefined(symb);
    }
  }


  private static void PopulateProductionHeads(GrammarTables.GrammarProductionList grammar_productions_, BuilderSymbolsListCtor out_builder_symbols_)
  {
    foreach (GrammarTables.GrammarProduction production in grammar_productions_)
      out_builder_symbols_.AddUniqueFromPopulateProductionHeads(production);
  }


  //TODO  мы должны не симболы по имени искать, а искать симболы ДЛЯ селф-симболов
  //      БОЛЬШОЕ ТУДУ - тут и поле Identificator вводить надо и поле SymbolCategory (а не метод) и конструкторы причесывать..
  private static BuilderProductionsList PopulateProductions(GrammarTables.GrammarProductionList grammar_productions_, BuilderSymbolsListCtor builder_symbols_)
  {
    BuilderProductionsList out_builder_productions = new BuilderProductionsList(grammar_productions_.Count());

    foreach (GrammarTables.GrammarProduction grammar_production in grammar_productions_)
    {
      //TODO  Здесь можно получить именно симбол, а не индекс, а потом симбол по индексу!
      int symb_non_terminal_index = (int)builder_symbols_.ItemIndex(grammar_production.Head, SymbolType.Nonterminal);
      var symb_non_terminal = builder_symbols_[symb_non_terminal_index];

      BuilderProduction build_production = new BuilderProduction(symb_non_terminal);
      foreach (GrammarTables.GrammarSymbol grammar_symbol in grammar_production.Handle)
      {
        int build_symbol_index = (grammar_symbol.Type == SymbolType.Nonterminal) ?
          builder_symbols_.ItemIndexCategory(grammar_symbol, SymbolCategory.Nonterminal)
          : builder_symbols_.ItemIndexCategory(grammar_symbol, SymbolCategory.Terminal);

        build_production.Handle().Add(builder_symbols_[build_symbol_index]);
      }
      out_builder_productions.Add(build_production);
    }

    return out_builder_productions;
  }
  

  private static void PopulateHandleSymbols(AppLog log_, GrammarTables.GrammarSymbolList grammar_handle_symbols_, BuilderSymbolsListCtor in_out_builder_symbols_)
  {
    foreach (GrammarTables.GrammarSymbol grammar_handle_symbol in grammar_handle_symbols_)
    {
      if (grammar_handle_symbol.Type == SymbolType.Nonterminal)
      {
        if (in_out_builder_symbols_.ItemIndexCategory(grammar_handle_symbol, SymbolCategory.Nonterminal) == (short) -1)
          log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Undefined rule: <" + grammar_handle_symbol.Name + ">", "", grammar_handle_symbol.Line.ToString());
      }
      else if (in_out_builder_symbols_.ItemIndexCategory(grammar_handle_symbol, SymbolCategory.Terminal) == (short) -1)
      {
        BuilderSymbol builder_symbol = in_out_builder_symbols_.AddSymbolHandleImplicitlyDefinedTerminal(grammar_handle_symbol);
        log_.Add(AppLogSection.Grammar, AppLogAlert.Detail, builder_symbol.Name + " was implicitly defined", "The terminal was implicitly declared as " + builder_symbol.Text());
      }
    }  
  }


  private static void PopulateGroupsAndWhitespace(AppLog log_, CharSetMode grammar_charsets_mode_, PreDefinedCharsetsList builder_predefined_sets_, BuilderPropertiesList builder_propirties_,
    GrammarTables.GrammarGroupsList in_grammar_groups_, BuilderSymbolsListCtor in_out_builder_symbols_, BuilderGroupsListCtor out_builder_groups_)
  {
    //TODO  А почему, если мы позже можем отредактировать чарсет charset_whitespaces, мы редактируем его копию?
    //      а потому что пре-дефайнед чарсет мы не должны мочь редактировать!
    //      т.е. объекты builder_predefined_sets_.Charset_Whitespace НЕ ДОЛЖНЫ иметь метод .Remove
    //      позже, по условию мы можем создать симбол-терминал-new-line на основе этого чарсета
    //      оч.. запутано
    BuilderFACharset charset_whitespaces;
    //
    if (grammar_charsets_mode_ == CharSetMode.ANSI)
      charset_whitespaces = builder_predefined_sets_.Charset_Whitespace.MakeCopy();
    else
      charset_whitespaces = builder_predefined_sets_.Charset_AllWhitespace.MakeCopy();

    //TODO  Это несколько странная метода вызывается только в том случае, если у нас есть non_block группы
    //      (т.е. line-группы, которые должны заканчиваться NewLine)
    //      и при этом, если NewLine симбол не определен в грамматике, то мы его втихую создаем
    //TODO  Как минимум мы должны сделать BuilderApp.Log.Add(AppLogSection.Grammar, AppLogAlert.Detail, "NewLine was implicitly defined" ...
    //      хотя я бы в этом случая ошибку бросил, или, еще лучше требовал явную опцию игнорирования этого предупреждения
    BuilderSymbol _get_or_create_symbol_NewLine_for_non_block_group_end_symbol()
    {
      BuilderSymbol? symbol_NewLine = in_out_builder_symbols_.Symbol_NewLine();
      if (symbol_NewLine != null)
        return symbol_NewLine;

      TerminalExpression regexp_NewLine = new TerminalExpression();
      charset_whitespaces.Remove(10, 13, 8232, 8233);
      regexp_NewLine.AddTextExpr("{LF}|{CR}{LF}?|{LS}|{PS}");
      symbol_NewLine = BuilderSymbolCtor.CreateSymbolNewLine(regexp_NewLine);
      in_out_builder_symbols_.AddSymbolNewLine(symbol_NewLine);

      return symbol_NewLine;
    }

    foreach (GrammarTables.GrammarGroup grammar_group in in_grammar_groups_)
    {
      BuilderSymbol new_group_container_symbol  = in_out_builder_symbols_.AddOrGetFromPopulateGroups_Container(grammar_group);
      BuilderSymbol new_group_start_symbol      = in_out_builder_symbols_.AddOrGetFromPopulateGroups_Start(grammar_group);
      GroupEndingMode new_group_ending_mode;
      BuilderSymbol     new_group_end_symbol;
      if (grammar_group.IsBlock)
      {
        new_group_ending_mode = GroupEndingMode.Closed;
        new_group_end_symbol = in_out_builder_symbols_.AddOrGetFromPopulateGroups_End(grammar_group);
      }
      else
      {
        new_group_ending_mode = GroupEndingMode.Open;
        new_group_end_symbol  = _get_or_create_symbol_NewLine_for_non_block_group_end_symbol();
      }
      //TODO  И зачем весь цикл выше, если мы заранее по имени группы можем определить, что новую группу создавать не будем ???        
      if (!out_builder_groups_.Contains(in grammar_group.Identificator))
        out_builder_groups_.Add(new BuilderGroupCtor(grammar_group.Name, new_group_container_symbol, new_group_start_symbol, new_group_end_symbol, new_group_ending_mode));
    }

    if (builder_propirties_.AutoWhitespace && !in_out_builder_symbols_.Contains(BuilderSymbol.s_Symbol_Whitespace_Ident))
    {
      TerminalExpression regexp_whitespaces = BuilderTablesConstructor.CreateBasicRegExpCharset(charset_whitespaces, KleeneOp.One_Or_More /*'+'*/);
      in_out_builder_symbols_.AddSymbolWhitespace(BuilderSymbolCtor.CreateWhitespace(regexp_whitespaces));
      log_.Add(AppLogSection.Grammar, AppLogAlert.Detail, "Whitespace was implicitly defined", 
        "The special terminal 'Whitespace' was implicitly defined as: {" + BuilderUtility.DisplayText(charset_whitespaces) + "}+");
    }

    //TODO  Странно как-то. Сначала сами добавляем, а потом тип меняем. Может сразу как надо добавлять?
    //TODO  Смысл в том, что выше мы ищем нр. s_Symbol_Whitespace без учета типа, а теперь мы ему насильно навязываем тип .Noise
    //      ..хз..
    ReclassifyTerminal(in_out_builder_symbols_, in BuilderSymbol.s_Symbol_Whitespace_Ident, SymbolType.Noise);
    ReclassifyTerminal(in_out_builder_symbols_, in BuilderSymbol.s_Symbol_Comment_Ident, SymbolType.Noise);
  }
  //
  private static void ReclassifyTerminal(BuilderSymbolsListCtor builder_symbols_, ref readonly TIdentificator identificator_, SymbolType symbol_new_type_if_found_)
  {
    BuilderSymbolCtor? symb = builder_symbols_.GetTerminalSymbolForReclassify(in identificator_);
    if (symb != null)
      builder_symbols_.Reclassify(symb, symbol_new_type_if_found_);
  }


  //TODO  Как-то не очень хорошо всю BuilderApp.BuildTables передавать только чтобы .SetStartSymbol сделать..
  private static BuilderSymbol? PopulateStartSymbol(AppLog log_, BuilderPropertiesList builder_properties_, BuilderSymbolsListCtor builder_symbols_)
  {
    string start_symbol_name = builder_properties_.StartSymbol;
    if (start_symbol_name.StartsWith("<") && start_symbol_name.EndsWith(">"))
      start_symbol_name = start_symbol_name.Substring(1, start_symbol_name.Length - 2);

    if (start_symbol_name.Empty())
      log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "No specified start symbol", "Please assign the \"Start Symbol\" parameter to the start symbol's name");
    else
    {
      BuilderSymbol? start_symbol = builder_symbols_.GetNonTerminalSymbolForStartSymbol(start_symbol_name);
      if (start_symbol == null)
        log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Invalid start symbol", "Please assign the \"Start Symbol\" parameter to the start symbol's name");
      else
        return start_symbol;
    }

    return null;
  }


  //TODO  Полный алес. Имея целый парсер разбирать выражения вручную - это полный алес..
  private static void ApplyGroupAttributes(AppLog log_, GrammarAttributesList grammar_groups_attributes_, BuilderGroupsList builder_groups_)
  {
    const string s_ADVANCE = "ADVANCE";
    const string s_TOKEN = "TOKEN";
    const string s_CHARACTER = "CHARACTER";
    const string s_NESTING = "NESTING";
    const string s_ALL = "ALL";
    const string s_SELF = "SELF";
    const string s_Self = "Self";
    const string s_All = "All";
    const string s_NONE = "NONE";
    const string s_None = "None";
    const string s_ENDING = "ENDING";
    const string s_OPEN = "OPEN";
    const string s_CLOSED = "CLOSED";

    foreach (GrammarAttribute group_attribute in grammar_groups_attributes_)
    {
      int group_index_for_attribute = builder_groups_.ItemIndex(group_attribute.Name);
      if (group_index_for_attribute == -1)
        log_.Add(AppLogSection.Grammar, AppLogAlert.Warning, "Attributes set for undefined terminal/self_grammar_group", 
          "\"" + group_attribute.Name + "\" was set, but it was not defined.");
      else
      {
        BuilderGroup group = builder_groups_[group_index_for_attribute];
        for(int attr_value_index = 0; attr_value_index < group_attribute.Values.Count; ++attr_value_index) 
        {
          GrammarAttrItem  grammarAttrib       = group_attribute.Values[attr_value_index];
          bool                  b_attribute_invalid = false;
          string attrCODE = grammarAttrib.Name.ToUpper();
          if ( attrCODE.Equals(s_ADVANCE))
          {
            string attrVALUE = grammarAttrib.Value().ToUpper();
            if (attrVALUE.Equals(s_TOKEN) )
              group.Advance = GroupAdvanceMode.Token;
            else if (attrVALUE.Equals(s_CHARACTER))
              group.Advance = GroupAdvanceMode.Character;
            else
              b_attribute_invalid = true;
          }
          else if (attrCODE.Equals(s_NESTING))
          {
            string attrVALUE = grammarAttrib.Value().ToUpper();
            if (attrVALUE.Equals(s_ALL))
            {
              group.NestingNames = s_All;
              for(int group_index = 0; group_index < builder_groups_.Count; ++group_index)
                group.Nesting.Add(group_index);
            }
            else if (attrVALUE.Equals(s_SELF))
            {
              group.Nesting.Add((int)group.TableIndex);
              group.NestingNames = s_Self;
            }
            else if (attrVALUE.Equals(s_NONE))
              group.NestingNames = s_None;
            else if (grammarAttrib.IsSet)
            {
              for(int attr_index = 0; attr_index < grammarAttrib.List.Count(); ++attr_index)
              {
                int group_index = builder_groups_.ItemIndex(grammarAttrib.List[attr_index]);
                if (group_index == -1)
                  log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Undefined self_grammar_group", "Nesting attribute assignment for the self_grammar_group " 
                    + group.Name + " is invalid. The following was specified: " + grammarAttrib.List[attr_index]);
                else
                  group.Nesting.Add(group_index);
              }

              group.NestingNames = grammarAttrib.Value();
            }
            else
              b_attribute_invalid = true;
          }
          else if (attrCODE.Equals(s_ENDING))
          {
            string attrVALUE = grammarAttrib.Value().ToUpper();
            if (attrVALUE.Equals(s_OPEN))
              group.Ending = GroupEndingMode.Open;
            else if (attrVALUE.Equals(s_CLOSED))
              group.Ending = GroupEndingMode.Closed;
            else
              b_attribute_invalid = true;
          }
          else
            b_attribute_invalid = true;

          if (b_attribute_invalid)
          {
            log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Invalid attribute", "In the attribute assignment for '" 
              + group_attribute.Name + "', the following was specified: " + grammarAttrib.Name + " = " + grammarAttrib.Value(), group_attribute.Line.ToString());
          }
        }
      }
    }
  }


  private static void ApplySymbolAttributes(AppLog log_, GrammarAttributesList grammar_symbols_attributes_, BuilderSymbolsListCtor builder_symbols_)
  {
    //TODO  вообще не в глобальную область константы выносить надо, а сделать так чтобы строковые константы вообще ненужны было
    const string s_TYPE     = "TYPE";
    const string s_NOISE    = "NOISE";
    const string s_CONTENT  = "CONTENT";
    const string s_SOURCE   = "SOURCE";
    const string s_VIRTUAL  = "VIRTUAL";
    const string s_LEXER    = "LEXER";

    foreach (GrammarAttribute symbolAttribute in grammar_symbols_attributes_)
    {
      int symbol_index_for_attribute = (int)builder_symbols_.ItemIndex(in symbolAttribute.Identificator);
      if (symbol_index_for_attribute == -1)
        log_.Add(AppLogSection.Grammar, AppLogAlert.Warning, "Attributes set for undefined terminal/self_grammar_group", 
          "\"" + symbolAttribute.Name + "\" was set, but it was not defined.");
      else
      {
        BuilderSymbol builder_symbol = builder_symbols_[symbol_index_for_attribute];
        if (builder_symbol.Type == SymbolType.Content | builder_symbol.Type == SymbolType.Noise | builder_symbol.Type == SymbolType.GroupEnd)
        {
          for(int attr_value_index = 0; attr_value_index < symbolAttribute.Values.Count; ++attr_value_index)
          {
            GrammarAttrItem symbol_attr           = symbolAttribute.Values[attr_value_index];
            string          symbol_attr_CODE      = symbol_attr.Name.ToUpper();
            bool            b_symbol_attr_invalid = false;
            if (symbol_attr_CODE.Equals(s_TYPE))
            {
              string attrVALUE = symbol_attr.Value().ToUpper();
              if (attrVALUE.Equals(s_NOISE))
                builder_symbols_.Reclassify(builder_symbol, SymbolType.Noise);
              else if (attrVALUE.Equals(s_CONTENT))
                builder_symbols_.Reclassify(builder_symbol, SymbolType.Content);
              else
                b_symbol_attr_invalid = true;
            }
            else if (symbol_attr_CODE.Equals(s_SOURCE))
            {
              string attrVALUE = symbol_attr.Value().ToUpper();
              if (attrVALUE.Equals(s_VIRTUAL))
              {
                builder_symbol.UsesDFA = false;
                log_.Add(AppLogSection.Grammar, AppLogAlert.Detail, builder_symbol.Name + " is a virtual terminal", 
                  "This terminal was entered into the symbol table, but not the Deterministic Finite Automata. As a result, tokens must be created at runtime by the developer or a specialized version of the Engine.");
              }
              else if (attrVALUE.Equals(s_LEXER) )
                builder_symbol.UsesDFA = true;
              else
                b_symbol_attr_invalid = true;
            }
            else
              b_symbol_attr_invalid = true;

            if (b_symbol_attr_invalid) 
              log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Invalid attribute", "In the attribute assignment for '" 
                + symbolAttribute.Name + "', the following was specified: " + symbol_attr.Name + " = " + symbol_attr.Value(), symbolAttribute.Line.ToString());
          }
        }
        else
          log_.Add(AppLogSection.Grammar, AppLogAlert.Warning, "Cannot change attributes", "The attributes for '" + symbolAttribute.Name + "' cannot be changed.");
      }
    }
  }


  private static void ApplyVirtualProperty(AppLog log_, BuilderPropertiesList builder_properties_, BuilderSymbolsListCtor out_builder_symbols_)
  {
    var prop_virtual_terminal_text = builder_properties_.VirtualTerminals;
    if (!prop_virtual_terminal_text.EmptyOrNull())
    {
      string[] prop_virtual_terminal_elements = prop_virtual_terminal_text.Split(' ');
      foreach (string virtual_terminal_text_raw in prop_virtual_terminal_elements)
      {
        string virtual_terminal_name = virtual_terminal_text_raw.Trim();
        if (!virtual_terminal_name.Empty())
        {
          (bool b_add, BuilderSymbol builder_symbol) = out_builder_symbols_.AddUniqueFromApplyVirtualProperty(virtual_terminal_name);
          if (b_add)
            log_.Add(AppLogSection.Grammar, AppLogAlert.Detail, virtual_terminal_name + " is a virtual terminal",
              "This terminal was entered into the symbol table, but not the Deterministic Finite Automata. As a result, tokens must be created at runtime by the developer or a specialized version of the Engine.");
          else
            log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, virtual_terminal_name + " virtual terminal name conflict", "This virtual terminal conflict with existing symbol already defined in grammar.");
        }
      }
    }
  }


  private static void CreateImplicitDeclarationsRegExp(BuilderSymbolsList in_out_builder_symbols_)
  {
    for(int symbol_index = 0; symbol_index < in_out_builder_symbols_.Count(); ++symbol_index)
    {
      BuilderSymbol symbol = in_out_builder_symbols_[symbol_index];
      if (symbol.Category() == SymbolCategory.Terminal && symbol.UsesDFA && symbol.RegularExp == null)
      {
        symbol.RegularExp = BuilderTablesConstructor.CreateBasicRegExpSequence(symbol.Name, KleeneOp.None);
        symbol.CreatedBy  = SymbolCreationType.Implicit;
      }
    }
  }
  //
  private static TerminalExpression CreateBasicRegExpSequence(string setitem_text_, KleeneOp kleene_op_)
  {
    TerminalExpression         regexp      = new TerminalExpression();
    TerminalExpressionSequence regexp_seq  = new TerminalExpressionSequence();
    regexp_seq.Add(new TerminalExpressionItem(CharsetExpressionItem.CreateItemSequence(setitem_text_), kleene_op_));
    regexp.Add(regexp_seq);
    return regexp;
  }
  private static TerminalExpression CreateBasicRegExpCharset(BuilderFACharset setitem_charset_, KleeneOp kleene_op_)
  {
    TerminalExpression         regexp      = new TerminalExpression();
    TerminalExpressionSequence regexp_seq  = new TerminalExpressionSequence();
    regexp_seq.Add(new TerminalExpressionItem(CharsetExpressionItem.CreateItemCharset(setitem_charset_), kleene_op_));
    regexp.Add(regexp_seq);
    return regexp;
  }


  private static void AssignTableIndexes(BuilderSymbolsListCtor builder_symbols_, BuilderProductionsList productions_, BuilderGroupsListCtor groups_)
  {
    builder_symbols_.SortSymbolsAndSetTableIndexes();

    for (int i = 0; i < productions_.Count(); ++i)
      productions_[i].SetTableIndex(to_short(i));

    groups_.AssignTableIndexes();
  }


  private static void LinkGroupSymbolsToGroup(BuilderGroupsList groups_)
  {
    for(int group_index = 0; group_index < groups_.Count ; ++group_index) 
    {
      BuilderGroup group = groups_[group_index];
      group.Container.SetGroupFromConstructor(group);
      group.Start.SetGroupFromConstructor(group);
      group.End.SetGroupFromConstructor(group);
    }
  }


  private static void AssignSymbolRegexpPriorities(AppLog log_, BuilderSymbolsList symbols_)
  {
    for(int symbol_index = 0; symbol_index < symbols_.Count(); ++symbol_index) 
    {
      BuilderSymbol symbol = symbols_[symbol_index];
      if (symbol.Category() == SymbolCategory.Terminal && symbol.RegularExp != null)
      {        
        TerminalExpression  symbol_regexp                       = symbol.RegularExp;
        bool    b_symbol_has_regexp_variable_lenght = false;
        for (int regexp_element_index = 0; regexp_element_index < symbol_regexp.Count(); ++regexp_element_index) 
        {
          TerminalExpressionSequence regexp_element = symbol_regexp[regexp_element_index];
          if (regexp_element.Priority == (short) -1)
          {
            if (regexp_element.IsVariableLength())
            {
              regexp_element.Priority = (short) 10001;
              b_symbol_has_regexp_variable_lenght = true;
            }
            else
              regexp_element.Priority = (short) 0;
          }
        }
        symbol.VariableLength = b_symbol_has_regexp_variable_lenght;
        if (b_symbol_has_regexp_variable_lenght)
          log_.Add(AppLogSection.Grammar, AppLogAlert.Detail, symbol.Name + " is variable length.");
      }
    }
  }


  private static void DoSemanticAnalysis(AppLog log_, BuilderTables builder_tables_, BuilderSymbol start_symbol_)
  {
    if (!log_.LoggedCriticalError())
      BuilderTablesConstructor.CheckIllegalSymbols(log_, builder_tables_.Production);
    if (!log_.LoggedCriticalError())
      BuilderTablesConstructor.CheckUnusedSymbols(log_, builder_tables_.Symbol, start_symbol_, builder_tables_.Production);
    if (!log_.LoggedCriticalError())
      BuilderTablesConstructor.CheckRuleRecursion(log_, builder_tables_.Production);
    if (log_.LoggedCriticalError())
      return;
    BuilderTablesConstructor.CheckDuplicateGroupStart(log_, builder_tables_.Symbol, builder_tables_.Group);
  }

  private static void CheckIllegalSymbols(AppLog log_, BuilderProductionsList productions_)
  {
    for(int production_index = 0; production_index < productions_.Count(); ++production_index) 
    {
      BuilderProduction production = productions_[production_index];      
      short production_handle_index = 0;
      while ( production_handle_index < production.Handle().Count())
      {
        BuilderSymbol production_handle_symbol = production.Handle()[(int) production_handle_index];
        switch (production_handle_symbol.Type)
        {
          case SymbolType.Nonterminal:
          case SymbolType.Content:
          case SymbolType.GroupEnd:
            checked { ++production_handle_index; }
            continue;
          case SymbolType.Noise:
            if (production_handle_symbol.Reclassified)
            {
              log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Noise terminal used in a grammar_production.", 
                "The symbol '" + production_handle_symbol.Name 
                  + "' is was changed by 'Noise' by the system. This is done for the terminal's named 'Whitespace' and 'Comment'. It was used in the grammar_production " + production.Text());
              goto case SymbolType.Nonterminal;
            }
            else
            {
              log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Noise terminal used in a grammar_production.", 
                "The symbol '" + production_handle_symbol.Name 
                  + "' is declared as Noise. This means it is ignored by the parser. It was used in the grammar_production " + production.Text());
              goto case SymbolType.Nonterminal;
            }
          case SymbolType.GroupStart:
            log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Cannot use a self_grammar_group start symbol in a grammar_production.", 
              "The symbol '" + production_handle_symbol.Name 
                + "' is the start of a lexical self_grammar_group. The lexer will use this symbol, and the matching end, to create a single container token. It was used in the grammar_production " + production.Text());
            goto case SymbolType.Nonterminal;
          default:
            log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Illegal symbol", 
              "The symbol '" + production_handle_symbol.Name 
                + "' is not allowed  in the grammar_production " + production.Text());
            goto case SymbolType.Nonterminal;
        }
      }
    }
  }


  private static void CheckUnusedSymbols(AppLog log_, BuilderSymbolsList symbols_, BuilderSymbol start_symbol_, BuilderProductionsList productions_)
  {
    //TODO  Битмап сюда надо
    bool[] tmp_symbol_is_used = new bool[symbols_.Count()];
    
    //TODO  Вроде ненадо - шарп и так все в 0 выставит ?
    for(int i = 0; i < symbols_.Count(); ++i)
      tmp_symbol_is_used[i] = false;

    BuilderTablesConstructor.CheckUnusedSymbols_CheckIfUsed(ref tmp_symbol_is_used, (int)start_symbol_.TableIndex, productions_);

    for(int symbol_index = 0; symbol_index <  symbols_.Count(); ++symbol_index) 
    {
      BuilderSymbol symbol = symbols_[symbol_index];
      if (!tmp_symbol_is_used[(int) symbol_index] && symbol.Type == SymbolType.Nonterminal)
        log_.Add(AppLogSection.Grammar, AppLogAlert.Warning, "Unreachable rule: <" + symbol.Name + ">", "The rule <" + symbol.Name + "> cannot be reached from the \"Start Symbol\".");
    }

    if (log_.LoggedCriticalError())
      return;

    for(int symbol_index = 0; symbol_index <  symbols_.Count(); ++symbol_index)
    {
      BuilderSymbol symbol = symbols_[symbol_index];
      if (!tmp_symbol_is_used[symbol_index] && symbol.Type == SymbolType.Content)
        log_.Add(AppLogSection.Grammar, AppLogAlert.Warning, "Unused terminal: " + symbol.Name, "The terminal " + symbol.Name + " is defined but not used.");      
    }
  }
  private static void CheckUnusedSymbols_CheckIfUsed(ref bool[] out_check_if_used_, int NonTerminalIndex, BuilderProductionsList productions_)
  {
    for (int production_index = 0; production_index < productions_.Count(); ++production_index)
    {
      BuilderProduction production = productions_[production_index];
      if ((int)production.Head.TableIndex == NonTerminalIndex)
      {
        out_check_if_used_[NonTerminalIndex] = true;
        //
        for (int production_handle_index = 0; production_handle_index < production.Handle().Count(); ++production_handle_index)
        {
          BuilderSymbol production_handle_symbol = production.Handle()[production_handle_index];
          switch (production_handle_symbol.Type)
          {
            case SymbolType.Nonterminal:
              if (!out_check_if_used_[(int)production_handle_symbol.TableIndex])
                BuilderTablesConstructor.CheckUnusedSymbols_CheckIfUsed(ref out_check_if_used_, (int)production_handle_symbol.TableIndex, productions_);
              break;
            case SymbolType.Content:
              out_check_if_used_[(int)production_handle_symbol.TableIndex] = true;
              break;
          }
        }
      }
    }
  }

  private static void CheckRuleRecursion(AppLog log_, BuilderProductionsList productions_)
  {
    for(int production_index = 0; production_index < productions_.Count(); ++production_index) 
    {
      BuilderProduction production = productions_[production_index];
      if (production.Handle().Count() == 1)
      {
        if ((int) production.Head.TableIndex == (int) production.Handle()[0].TableIndex)
          log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "The rule " + production.Name() 
            + " is defined as itself", "This rule is defined using the form <A> ::= <A>. This will eventually lead to a shift-reduce error.");
      }
      else if (production.Handle().Count() >= 2)
      {
        table_index_t production_head_symbol_index        = production.Head.TableIndex;
        table_index_t production_handle_1st_symbol_index  = production.Handle()[0].TableIndex;
        table_index_t production_handle_last_symbol_index = production.Handle()[production.Handle().Count() - 1].TableIndex;
        if (production_head_symbol_index == production_handle_1st_symbol_index && production_head_symbol_index == production_handle_last_symbol_index)
          log_.Add(AppLogSection.Grammar, AppLogAlert.Warning, "The rule " + production.Name() 
            + " is both left and right recursive.", "This rule is defined using the form <A> ::= <A> .. <A>. This will eventually lead to a shift-reduce error.");
      }
    }
  }

  private static void CheckDuplicateGroupStart(AppLog log_, BuilderSymbolsList symbols_, BuilderGroupsList groups_)
  {
    //TODO  А есть ли целесообразность только для проверки целый массив СПИСКОВ групп создавать ??
    //      И почему + 1 ?? на всякий случай что ли ??
    BuilderGroupsListCtor[] tmp_group_lists_array = new BuilderGroupsListCtor[symbols_.Count() + 1];

    for(int symbol_index = 0; symbol_index < symbols_.Count(); ++symbol_index)
      tmp_group_lists_array[(int) symbol_index] = new BuilderGroupsListCtor();

    for(int group_index = 0; group_index < groups_.Count;  ++group_index) 
    {
      BuilderGroup group = groups_[group_index];
      tmp_group_lists_array[group.Start.TableIndex].AddTmp(group);
    }

    for(int symbol_index = 0; symbol_index < symbols_.Count(); ++symbol_index)
    {
      if (tmp_group_lists_array[(int) symbol_index].Count >= 2)
      {
        BuilderSymbol symbol = symbols_[symbol_index];
        log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, 
          "Symbol used to start multiple groups", "The symbol '" + symbol.Name 
          + "' is used in the following groups: " + tmp_group_lists_array[symbol_index].ToString());
      }
    }
  }

}

