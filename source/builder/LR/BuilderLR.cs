//
using System.Diagnostics;

//
//
namespace gpp.builder;


internal sealed class BuilderLR
{
  private BuilderLRConfigSet[]         GotoList;
  private BuilderLRConfigSetLookup     m_LRConfigSetLookup;
  //
  private AppNotify                     m_app_notify;
  private AppLog                        m_app_log;
  private BuilderSymbolsList            m_Symbols;
  private BuilderProductionsList        m_Productions;
  private BuilderLRStatesList           m_LALR;
  private AppLog                        _LOG          => m_app_log;
  private AppNotify                     _NOTIFY       => m_app_notify;
  private BuilderSymbolsList            _SYMBOLS      => m_Symbols;
  private BuilderProductionsList        _PRODUCTIONS  => m_Productions;
  private BuilderLRStatesList           _LALR         => m_LALR;
  private BuilderLRConfigSetLookup      _LRConfigSetLookup => m_LRConfigSetLookup;


  public static string GetConflictName(BuilderLRConflict conflict_)
  {
    string conflictName;
    switch (conflict_)
    {
      case BuilderLRConflict.None:
        conflictName = "None";
        break;
      case BuilderLRConflict.ShiftReduce:
        conflictName = "Shift-Reduce";
        break;
      case BuilderLRConflict.ReduceReduce:
        conflictName = "Reduce-Reduce";
        break;
      case BuilderLRConflict.AcceptReduce:
        conflictName = "Accept-Reduce";
        break;
      default:
        conflictName = "Unknown";
        break;
    }
    return conflictName;
  }

  public static string GetConflictDesc(BuilderLRConflict conflict_)
  {
    string conflictDesc;
    switch (conflict_)
    {
      case BuilderLRConflict.None:
        conflictDesc = "There is no conflict.";
        break;
      case BuilderLRConflict.ShiftReduce:
        conflictDesc = "A Shift-Reduce Conflict is caused when the system cannot " 
                        + "determine whether to advance (shift) one rule or complete " 
                        + "(reduce) another. This means that the *same* text can be parsed " 
                        + "into two or more distrinct trees at the same time. " 
                        + "The grammar is ambigious. " 
                        + "Please see the documentation for more information.";
        break;
      case BuilderLRConflict.ReduceReduce:
        conflictDesc = "A Reduce-Reduce error is a caused when a grammar allows " 
                        + "two or more rules to be reduced at the same time, for the " + "same token. " 
                        + "The grammar is ambigious. " 
                        + "Please see the documentation for more information.";
        break;
      case BuilderLRConflict.AcceptReduce:
        conflictDesc = "This NEVER happens";
        break;
      default:
        conflictDesc = "Unknown";
        break;
    }
    return conflictDesc;
  }

  public static string GetConflictResolvedDesc(BuilderLRConflict Conflict)
  {
    string conflictResolvedDesc;
    switch (Conflict)
    {
      case BuilderLRConflict.None:
        conflictResolvedDesc = "";
        break;
      case BuilderLRConflict.ShiftReduce:
        conflictResolvedDesc = "The conflict was resolved by selecting the 'shift' action over the 'reduce'. " 
                                + "Be careful, some parts grammar may not be accessable. " 
                                + "It is recommended that you attempt to remove all conflicts.";
        break;
      default:
        conflictResolvedDesc = "This conflict cannot be automatically resolved";
        break;
    }
    return conflictResolvedDesc;
  }

  private BuilderLRState CreateInitialState(BuilderSymbol start_symbol_)
  {
    BuilderSymbol   symb_special_non_terminal = BuilderSymbol.CreateSpecialNonTerminal();    
    BuilderProduction initial_production        = new BuilderProduction(symb_special_non_terminal);
    initial_production.Handle().Add(start_symbol_);
    initial_production.Handle().Add(_SYMBOLS.GetSymbol_End());

    BuilderLRState initial_state = new BuilderLRState();
    initial_state.ConfigSet.Add(new LRConfig(initial_production));

    BuilderLR.Closure(ref initial_state.ConfigSet);
    return initial_state;
  }

  private int CreateLRState(BuilderLRState lr_state_new_)
  {
    //TODO  Это плохая функция.
    //      1) Мы сначала гоняем хэш чтобы найти этот lr_state_new_, а потом, если не найдено опять гоняем хэш чтобы добавить
    //      2) .CompareCore достаточно "жирный" метод, но мы его используем только для ..== LRConfigCompare.EqualBaseNotEqualLookahead
    //          для собственно .EqualBaseNotEqualLookahead там можно проще сделать
    //TODO  Также для п.2 при .UnionWith мы всегда устанавливаем .Modified = true, хотя, возможно, это не всегда верно
    //      ПС. там где-то в .UnionWith какая-то проблемка есть - емнип оно всегда возвращает false.. поглядеть надо.. или не тут.. а может этот .Modified = true вообще нафиг ненужен??
    int lr_state_index = _LRConfigSetLookup.get_TableIndex(lr_state_new_.ConfigSet);
    if (lr_state_index < 0)
    {
      lr_state_new_.Expanded = false;
      lr_state_new_.Modified = true;
      lr_state_index = _LALR.Add(lr_state_new_);
      _LRConfigSetLookup.Add(lr_state_new_.ConfigSet, lr_state_index);
      ++_NOTIFY.Counter;
    }
    else if(lr_state_new_.ConfigSet.CompareCore(_LALR[lr_state_index].ConfigSet) == LRConfigCompare.EqualBaseNotEqualLookahead)
    {
      BuilderLRState lr_state = _LALR[(int)lr_state_index];
      lr_state.ConfigSet.UnionWith(lr_state_new_.ConfigSet);
      lr_state.Modified = true;
    }

    return lr_state_index;
  }


  public static (bool, BuilderLRStatesList) DoBuild(AppNotify app_notify_, AppLog app_log_, BuilderSymbolsList symbols_, BuilderSymbol start_symbol_, BuilderProductionsList productions_) 
  {
    Debug.Assert(start_symbol_ != null && (start_symbol_.TableIndex >=0 && start_symbol_.TableIndex < symbols_.Count()));

    return new BuilderLR(app_notify_, app_log_, symbols_, productions_).Build(start_symbol_);
  }
  private BuilderLR(AppNotify app_notify_, AppLog app_log_, BuilderSymbolsList symbols_, BuilderProductionsList productions_)
  {
    m_app_notify        = app_notify_;
    m_app_log           = app_log_;
    m_Symbols           = symbols_;
    m_Productions       = productions_;
    m_LRConfigSetLookup = new BuilderLRConfigSetLookup();
    m_LALR              = new BuilderLRStatesList();
    this.GotoList       = new BuilderLRConfigSet[symbols_.Count() + 1];  //TODO Почему +1 ?? Может -1 надо ??
  }

  private (bool, BuilderLRStatesList) Build(BuilderSymbol start_symbol_)
  {
    _NOTIFY.Mode = AppProgramMode.BuildingLALR;

    size_t count_critical_at_begin = _LOG.AlertCount(AppLogAlert.Critical);

    if (start_symbol_ == null || start_symbol_.TableIndex < 0)
    {
      _LOG.Add(AppLogSection.LALR, AppLogAlert.Critical, "Start Symbol is invalid or missing");
      return (false, _LALR);
    }

    //TODO  Это метод подготовки к вычислениям. Он модифицирует поля в таблице Симболов SetupNullableTable и SetupFirstTable
    //      Его бы вынести отсюда.
    this.SetupTempTables();

    if (start_symbol_.First.Count() == 0)
    {
      _LOG.Add(AppLogSection.LALR, AppLogAlert.Critical, "initial_production <" + start_symbol_.Name + "> does not produce any terminals.");
      return (false, _LALR);
    }
      
    BuilderLRState lr_state_new_initial_ = this.CreateInitialState(start_symbol_);
    this.CreateLRState(lr_state_new_initial_);

    // если мы нашли хоть один !lr_state_new_.Expanded, то сигнализируем об этом, чтобы внешний цикл прокрутился еще один раз
    //TODO  при выполнении .ComputeLRState(lr_state_new_) в список .BuildTables.LALR могут быть добевлены еще новые элементы (!lr_state_new_.Expanded)
    //      поэтому если такое происходит, мы прорходим весь список еще раз
    //TODO  .ComputeLRState в некоторых случаях делает .BuildLR.CreateLRState, что увеличивает список .BuildTables.LALR добавляя в конец элемент
    //      и нам бы не прокручивать весь цикл еще раз, а вернуть добавленный элемент из .ComputeLRState (он будет !lr_state_new_.Expanded, видимо..)
    //      и зациклиться while пока .ComputeLRState возвращает нам новые элементы
    bool b_not_expanded_state_found_and_maybe_lalr_list_changed;
    do
    {
      b_not_expanded_state_found_and_maybe_lalr_list_changed = false;
      for (int lalr_index = 0; lalr_index < _LALR.Count; ++lalr_index)
      {
        BuilderLRState lr_state = _LALR[lalr_index];
        if (!lr_state.Expanded)
        {
          this.ComputeLRState(lr_state);
          lr_state.Expanded = true;
          b_not_expanded_state_found_and_maybe_lalr_list_changed = true;
        }
      }
    }
    while (b_not_expanded_state_found_and_maybe_lalr_list_changed);

    bool b_modified_state_found_and_maybe_another_state_modified;
    do
    {        
      b_modified_state_found_and_maybe_another_state_modified = false;
      for (int lalr_index = 0; lalr_index < _LALR.Count; ++lalr_index)
      {
        BuilderLRState lr_state = _LALR[lalr_index];
        if (lr_state.Modified)
        {
          //TODO  Опять же .RecomputeLRState может сделать .Modified какие-то другие Стейт (возможно не один, а весь список его ParserLRAction)
          //      и мы вынуждены будем заново перепросматривать весь массив .BuildTables.LALR на предмет модифицированных
          this.RecomputeLRState(lr_state);
          lr_state.Modified = false;
          b_modified_state_found_and_maybe_another_state_modified = true;                        
        }
      }
    }
    while (b_modified_state_found_and_maybe_another_state_modified);

    this.ComputeReductions();

    bool b_build_lr_no_error = (_LOG.AlertCount(AppLogAlert.Critical) == count_critical_at_begin);
    if (b_build_lr_no_error)
    {
      _LOG.Add(AppLogSection.LALR, AppLogAlert.Success, "LALR Table was succesfully created", "The table contains a total of " + _LALR.Count.ToString() + " states");
      BuilderUtility.LogActionTotals(_LOG, _LALR);
    }

    return (b_build_lr_no_error, _LALR);
  }


  private void ComputeLRState(BuilderLRState State)
  {
    for (int symbol_index = 0; symbol_index < _SYMBOLS.Count(); ++symbol_index) 
      this.GotoList[symbol_index] = (BuilderLRConfigSet)null;
    
    for (int config_set_index = 0; config_set_index < State.ConfigSet.Count(); ++config_set_index) 
    {
      LRConfig      lr_config             = State.ConfigSet[config_set_index];
      BuilderSymbol?  lr_config_next_symbol = lr_config.NextSymbol();
      if (lr_config_next_symbol != null)
      {
        int lr_config_next_symbol_index = lr_config_next_symbol.TableIndex;
        if (this.GotoList[lr_config_next_symbol_index] == null)
          this.GotoList[lr_config_next_symbol_index] = new BuilderLRConfigSet();
        LRConfig lr_config_new = new LRConfig(lr_config.ParentProduction, lr_config.Position + 1, lr_config.LookaheadSet);
        this.GotoList[lr_config_next_symbol_index].Add(lr_config_new);
      }
    }

    for (int symbol_index = 0; symbol_index < _SYMBOLS.Count(); ++symbol_index)
    {
      if (this.GotoList[symbol_index] != null)
      {
        BuilderLR.Closure(ref this.GotoList[symbol_index]);  //TODO  Здесь медленно

        BuilderLRState lr_state_new = new BuilderLRState();
        lr_state_new.ConfigSet = this.GotoList[symbol_index];

        BuilderSymbol TheSymbol = _SYMBOLS[symbol_index];
        switch (TheSymbol.Type)
        {
          case SymbolType.Nonterminal:
            State.CreateActionIfAbsentOrIfConflict(TheSymbol, LRActionType.Goto, this.CreateLRState(lr_state_new));
            break;
          case SymbolType.End:
            State.CreateActionIfAbsentOrIfConflict(TheSymbol, LRActionType.Accept, BuilderLRAction.LR_ACTION_VALUE_DEFAULT);
            break;
          default:
            State.CreateActionIfAbsentOrIfConflict(TheSymbol, LRActionType.Shift, this.CreateLRState(lr_state_new));
            break;
        }
        ++_NOTIFY.Analyzed;
      }
    }

  }


  private void SetupTempTables()
  {
    _NOTIFY.Mode = AppProgramMode.BuildingFirstSets;
    this.SetupNullableTable();
    this.SetupFirstTable();

    _NOTIFY.Mode = AppProgramMode.BuildingLALRClosure;
    this.ComputePartialClosures();    
  }


  private void RecomputeLRState(BuilderLRState lr_state_)
  {
    for (int lr_state_action_index = 0; lr_state_action_index < lr_state_.CountOfLRActions(); ++lr_state_action_index)
    {
      BuilderLRAction lr_state_action = lr_state_[(short)lr_state_action_index];
      switch (lr_state_action.Type)
      {
        case LRActionType.Shift:
        case LRActionType.Goto:
          BuilderLRState lrStateBuild1 = BuilderLR.GotoSymbol(lr_state_, (BuilderSymbol)lr_state_action.Symbol);
          BuilderLRState lrStateBuild2 = _LALR[lr_state_action.Value()];
          if (lrStateBuild1.ConfigSet.CompareCore(lrStateBuild2.ConfigSet) == LRConfigCompare.EqualBaseNotEqualLookahead)
          {
            lrStateBuild2.ConfigSet.UnionWith(lrStateBuild1.ConfigSet);
            lrStateBuild2.Modified = true;
          }
          ++_NOTIFY.Analyzed;
          break;
      }
    }
  }


  public static void Closure(ref BuilderLRConfigSet in_out_config_set_)
  {
    int in_out_config_set_count = in_out_config_set_.Count();
    if (in_out_config_set_count == 0)
      return;

    BuilderLRConfigSet config_set_addition = new BuilderLRConfigSet();

    for(int config_index = 0; config_index < in_out_config_set_.Count(); ++config_index)
    {
      LRConfig            config = in_out_config_set_[config_index];
      //TODO  .TotalLookahead вызываем всегда, а используем только если 'if (config_next_symbol != null && config_next_symbol.Type == SymbolType.Nonterminal)' - он не дешевый
      BuilderLRLookaheadSymbolSet  config_total_lookahead_symbolset  = BuilderLR.TotalLookahead(config);
      BuilderSymbol?        config_next_symbol                = config.NextSymbol();
      if (config_next_symbol != null && config_next_symbol.Type == SymbolType.Nonterminal)
      {
        BuilderLRConfigSet config_next_symbol_partial_closure = config_next_symbol.PartialClosure;
        for(int config_next_symbol_partial_closure_index = 0; config_next_symbol_partial_closure_index < config_next_symbol_partial_closure.Count(); ++config_next_symbol_partial_closure_index)
        {
          LRConfig lrConfig1 = config_next_symbol_partial_closure[config_next_symbol_partial_closure_index];
          LRConfig lrConfig2 = new LRConfig(lrConfig1.ParentProduction, 0, lrConfig1.LookaheadSet);
          if (lrConfig1.InheritLookahead)
            lrConfig2.LookaheadSet.UnionWith(config_total_lookahead_symbolset);
          config_set_addition.Add(lrConfig2);
        }
      }
    }

    in_out_config_set_.UnionWith(config_set_addition);
  }


  private void ComputePartialClosures()
  {
    int symbols_count = _SYMBOLS.Count();
    //TODO  Подсчет количества нон-терминалов в грамматике можно сделать где-то прямо при создании симболов и сохранять, а не крутить цикл отдельно
    int symbols_nonterminal_count = 0;
    //
    for (int symbol_index = 0; symbol_index < symbols_count; ++symbol_index)
    {
      if (_SYMBOLS[symbol_index].Type == SymbolType.Nonterminal)
        ++symbols_nonterminal_count;
    }

    string symbols_nonterminal_count_of_suffix = " of " + symbols_nonterminal_count; 
    int symbol_nonterminal_n = 0;
    for (int symbol_index = 0; symbol_index < symbols_count; ++symbol_index)
    {
      BuilderSymbol symbol = _SYMBOLS[symbol_index];
      if (symbol.Type == SymbolType.Nonterminal)
      {
        ++symbol_nonterminal_n;
        //TODO  Строковые операции в цикле только с целью информирования - плохо для производительности. Желательно уметь отключать это.
        _NOTIFY.Text = symbol_nonterminal_n.ToString() + symbols_nonterminal_count_of_suffix;
        symbol.PartialClosure = this.GetClosureConfigSet(symbol);
      }
    }
  }


  private BuilderLRConfigSet GetClosureConfigSet(BuilderSymbol symbol_to_make_closure_config_set_)
  {    
    BuilderLRConfigSet resulting_closure_config_set = new BuilderLRConfigSet();
    int productions_count = _PRODUCTIONS.Count();

    for (int production_index = 0; production_index < productions_count; ++production_index)
    {
      BuilderProduction production = _PRODUCTIONS[production_index];
      if (production.Head./*IsEqualKeyTo*/IsTheSame(symbol_to_make_closure_config_set_))
        resulting_closure_config_set.Add( new LRConfig(production, position_: 0, modified_: true, inherit_lookahead_: true) );
    }
   
    BuilderLRConfigSet SetB = new BuilderLRConfigSet();
    //
    do
    {
      SetB.Clear();
      //TODO  Здесь опять и опять после .UnionWith(SetB) в конце мы заново перепросматриваем весь config_set
      for (int closure_config_set_index = 0; closure_config_set_index < resulting_closure_config_set.Count(); ++closure_config_set_index)
      {
        LRConfig closure_config1 = resulting_closure_config_set[closure_config_set_index];
        if (!closure_config1.IsComplete() && closure_config1.Modified)
        {
          BuilderSymbol closure_config_next_symbol = closure_config1.NextSymbol();
          if (closure_config_next_symbol.Type == SymbolType.Nonterminal)
          {            
            for(int production_index = 0; production_index < productions_count; ++production_index)
            {
              BuilderProduction parent_production = _PRODUCTIONS[production_index];
              if (parent_production.Head./*IsEqualKeyTo*/IsTheSame(closure_config_next_symbol))
                SetB.Add(new LRConfig(parent_production, 0, BuilderLR.TotalLookahead(closure_config1)));
            }
          }
          closure_config1.Modified = false;
        }
      }
    }
    while (resulting_closure_config_set.UnionWith(SetB));

    int resulting_closure_config_set_сount = resulting_closure_config_set.Count();
    bool b_some_closure_config_with_inherit_lookahead_found;
    do
    {
      b_some_closure_config_with_inherit_lookahead_found = false;
      for (int closure_config_set_index = 0; closure_config_set_index < resulting_closure_config_set_сount; ++closure_config_set_index)
      {
        LRConfig      closure_config_1st              = resulting_closure_config_set[closure_config_set_index];
        BuilderSymbol?  closure_config_1st_next_symbol  = closure_config_1st.NextSymbol();
        if (closure_config_1st_next_symbol != null && closure_config_1st_next_symbol.Type == SymbolType.Nonterminal && closure_config_1st.InheritLookahead && BuilderLR.PopulateLookahead(closure_config_1st))
        {
          for (int closure_config_set_index2 = 0; closure_config_set_index2 < resulting_closure_config_set_сount; ++closure_config_set_index2)
          {
            LRConfig closure_config_2nd = resulting_closure_config_set[closure_config_set_index2];
            if (closure_config_2nd.Position == 0 && !closure_config_2nd.InheritLookahead && closure_config_2nd.ParentProduction.Head./*IsEqualKeyTo*/IsTheSame(closure_config_1st_next_symbol))
            {
              closure_config_2nd.InheritLookahead = true;
              b_some_closure_config_with_inherit_lookahead_found = true;
            }
          }
        }
      }
    }
    while (b_some_closure_config_with_inherit_lookahead_found);

    return resulting_closure_config_set;
  }


  private void ComputeReductions()
  {
    //TODO  А почему массив конфликтов на один тбольше чем симболов? Внизу вроде и заполнение его и итерирование идет до .Symbol.Count() ?
    BuilderLRConflictItem[] lrConflictItemArray = new BuilderLRConflictItem[checked (_SYMBOLS.Count() + 1)];

    for(int lr_state_index = 0; lr_state_index < _LALR.Count; ++lr_state_index)
    {
      BuilderLRState lr_state = _LALR[(int) lr_state_index];

      //TODO  А почему массив возможных конфликтов перезаполняется новыми для каждого LALR, а не просто очищается?
      for (int symbol_index = 0; symbol_index < _SYMBOLS.Count(); ++symbol_index)
      {
        lrConflictItemArray[symbol_index] = new BuilderLRConflictItem(_SYMBOLS[symbol_index]);
      }

      for(int lr_state_config_index = 0; lr_state_config_index < lr_state.ConfigSet.Count(); ++lr_state_config_index)
      {
        LRConfig lr_state_config = lr_state.ConfigSet[lr_state_config_index];
        switch (lr_state_config.NextAction())
        {
          case LRActionType.Shift:
            //TODO  При .NextAction() .Goto и .Shift .NextSymbol() не НУЛЛ. Но это неочевидно
            lrConflictItemArray[lr_state_config.NextSymbol()!.TableIndex].Shifts.Add(lr_state_config);  
            break;
          case LRActionType.Reduce:
            for (int lr_state_config_lookahead_index = 0; lr_state_config_lookahead_index < lr_state_config.LookaheadSet.Count(); ++lr_state_config_lookahead_index)
              lrConflictItemArray[lr_state_config.LookaheadSet[lr_state_config_lookahead_index].ParentSymbol.TableIndex].Reduces.Add(lr_state_config);
            break;
        }
      }

      //TODO  эта строка и весь код ниже нужен только если есть конфликты
      string lr_state_index_as_string = lr_state_index.ToString();

      for (int lr_conflict_item_index = 0; lr_conflict_item_index < _SYMBOLS.Count(); ++lr_conflict_item_index)
      {        
        if (lrConflictItemArray[(int)lr_conflict_item_index].Shifts.Count() > 0 && lrConflictItemArray[lr_conflict_item_index].Reduces.Count() > 0)
        {
          //TODO  А почему .Status = LRStatus.Warning не присваивать выше сразу при создании конфликта ?
          for (int lr_conflict_item_shifts_index = 0; lr_conflict_item_shifts_index < lrConflictItemArray[lr_conflict_item_index].Shifts.Count(); ++lr_conflict_item_shifts_index)
            lrConflictItemArray[lr_conflict_item_index].Shifts[lr_conflict_item_shifts_index].Status = LRStatus.Warning;            
          for (int lr_conflict_item_reduces_index = 0; lr_conflict_item_reduces_index < lrConflictItemArray[lr_conflict_item_index].Reduces.Count(); ++lr_conflict_item_reduces_index)
            lrConflictItemArray[lr_conflict_item_index].Reduces[lr_conflict_item_reduces_index].Status = LRStatus.Warning;

          lr_state.Status = LRStatus.Warning;
          lr_state.Note = "Shift-Reduce Conflict";
          lr_state.ConflictList.Add(new BuilderLRConflictItem(lrConflictItemArray[lr_conflict_item_index], BuilderLRConflict.ShiftReduce));
          string str = _SYMBOLS[lr_conflict_item_index].Text();
          string conflictResolvedDesc = BuilderLR.GetConflictResolvedDesc(BuilderLRConflict.ShiftReduce);
          string Description = str + " can follow a completed rule and also be shifted." + conflictResolvedDesc;
          _LOG.Add(AppLogSection.LALR, AppLogAlert.Warning, "A Shift-Reduce conflict_ was fixed", Description, lr_state_index_as_string);
        }
      }

      for(int lr_conflict_item_index = 0; lr_conflict_item_index < _SYMBOLS.Count(); ++lr_conflict_item_index)
      {
        if (lrConflictItemArray[lr_conflict_item_index].Reduces.Count() == 1 && lrConflictItemArray[lr_conflict_item_index].Shifts.Count() == 0)
        { //TODO  Это видимо так выглядит разрешение шифт-редюс конфликта? или что?
          BuilderSymbol conflict_symbol = _SYMBOLS[lr_conflict_item_index];
          lr_state.CreateActionIfAbsentOrIfConflict(conflict_symbol, LRActionType.Reduce, lrConflictItemArray[lr_conflict_item_index].Reduces[0].ParentProduction.TableIndex);
        }
        else if (lrConflictItemArray[lr_conflict_item_index].Reduces.Count() > 1)
        {
          for (int lr_conflict_item_reduces_index = 0; lr_conflict_item_reduces_index < lrConflictItemArray[lr_conflict_item_index].Reduces.Count(); ++lr_conflict_item_reduces_index)
            lrConflictItemArray[lr_conflict_item_index].Reduces[(int)lr_conflict_item_reduces_index].Status = LRStatus.Critical;

          lr_state.Status = LRStatus.Critical;
          lr_state.Note = "Reduce-Reduce Conflict";
          lr_state.ConflictList.Add(new BuilderLRConflictItem(lrConflictItemArray[lr_conflict_item_index], BuilderLRConflict.ReduceReduce));
          string str = _SYMBOLS[lr_conflict_item_index].Text();
          string conflictDesc = BuilderLR.GetConflictDesc(BuilderLRConflict.ReduceReduce);
          string Description = str + " can follow more than one completed rule. " + conflictDesc;
          _LOG.Add(AppLogSection.LALR, AppLogAlert.Critical, "Reduce-Reduce Conflict", Description, lr_state_index_as_string);
        }
      }
    }
  } //ComputeReductions


  private static BuilderLRState GotoSymbol(BuilderLRState lr_state_, BuilderSymbol symbol_)
  {
    BuilderLRState new_lr_state_ = new BuilderLRState();
    
    for (int config_set_index = 0; config_set_index < lr_state_.ConfigSet.Count(); ++config_set_index)
    {
      LRConfig      lr_state_config             = lr_state_.ConfigSet[config_set_index];
      BuilderSymbol?  lr_state_config_next_symbol = lr_state_config.NextSymbol();
      if (lr_state_config_next_symbol != null && lr_state_config_next_symbol.IsTheSame(symbol_))
        new_lr_state_.ConfigSet.Add(new LRConfig(lr_state_config.ParentProduction, lr_state_config.Position + 1, lr_state_config.LookaheadSet));
    }

    if (new_lr_state_.ConfigSet.Count() >= 1)
      BuilderLR.Closure(ref new_lr_state_.ConfigSet);

    return new_lr_state_;
  }





  private void SetupFirstTable()
  {
    for(int symbol_index = 0; symbol_index < _SYMBOLS.Count(); ++symbol_index)
      _SYMBOLS[symbol_index].First.Clear();

    for (int symbol_index = 0; symbol_index < _SYMBOLS.Count(); ++symbol_index)
    {
      BuilderSymbol Sym = _SYMBOLS[symbol_index];
      if (Sym.Type != SymbolType.Nonterminal)
        Sym.First.Add(new LRLookaheadSymbol(Sym));
    }

    //TODO Представляется здесь критическое место. Мы крутим цикл по всем продукциям и их _handle если хоть где-то удалось 
    //     объединить (production.HeadSymbol.First.UnionWith(production_handle_symbol.First), то перезапускаем цикл заново.
    //      Операция .UnionWith недешевая и крутиться на ней до победного конца каждый раз начиная сначала заново - ???
    //      Зачем так? Она имеет сайд-эффекты? Какие?
    bool b_some_production_head_first_lookahead_symbol_set_was_changed;
    do
    {
      b_some_production_head_first_lookahead_symbol_set_was_changed = false;
      for (int production_index = 0; production_index < _PRODUCTIONS.Count(); ++production_index)
      {
        BuilderProduction production = _PRODUCTIONS[production_index];        
        for (int production_handle_index = 0; production_handle_index < production.Handle().Count(); ++production_handle_index)
        {
          BuilderSymbol production_handle_symbol = production.Handle()[production_handle_index];
          if (production.Head.First.UnionWith(production_handle_symbol.First))
            b_some_production_head_first_lookahead_symbol_set_was_changed = true;
          if (!production_handle_symbol.Nullable)
            break;
        }
      }
    }
    while (b_some_production_head_first_lookahead_symbol_set_was_changed);
  }


  private void SetupNullableTable()
  {
    for(int symbol_index = 0; symbol_index < _SYMBOLS.Count(); ++symbol_index)
      _SYMBOLS[symbol_index].Nullable = false;

    //TODO  По виду весь внутренний цикл по продукциям без сайд-эффектов
    //      Зачем перезапускать его еще раз?
    bool b_production_to_set_head_nullable_found;
    do
    {
      b_production_to_set_head_nullable_found = false;

      for (int production_index = 0; production_index < _PRODUCTIONS.Count(); ++production_index)
      {
        BuilderProduction production = _PRODUCTIONS[production_index];
        
        bool b_production_handle_only_nullable = true;
        //
        for (int production_handle_index = 0; production_handle_index < production.Handle().Count(); ++production_handle_index)
        {
          if (!production.Handle()[production_handle_index].Nullable)
          {
            b_production_handle_only_nullable = false;
            break;
          }            
        }

        if (b_production_handle_only_nullable & !production.Head.Nullable)
        {
          production.Head.Nullable = true;
          b_production_to_set_head_nullable_found = true;
        }
      }
    }
    while (b_production_to_set_head_nullable_found);
  }


  static ConfigTrackSource GetConfigTrackSourceModeForSymbol(BuilderSymbol symbol_) 
  {
    if (symbol_.Type == SymbolType.Nonterminal)
      return ConfigTrackSource.First;
    else
      return ConfigTrackSource.Config;
  }
  private static BuilderLRLookaheadSymbolSet TotalLookahead(LRConfig config_)
  {
    BuilderLRLookaheadSymbolSet lookaheadSymbolSet = new BuilderLRLookaheadSymbolSet();

    bool b_union_with_config_lookahead_set_flag = true;
    for (int checkahead_index = 0; checkahead_index < (int) config_.CheckaheadCount(); ++checkahead_index)
    {        
      BuilderSymbol checkahead_symbol = config_.Checkahead(checkahead_index);
      for (int checkahead_symbol_first_index = 0; checkahead_symbol_first_index < checkahead_symbol.First.Count(); ++checkahead_symbol_first_index)
      {
        //TODO  А почему создаем его всегда, а используем только по условию? Сайд-эффекты?
        LRLookaheadSymbol lookaheadSymbol = new LRLookaheadSymbol(checkahead_symbol.First[checkahead_symbol_first_index]);
        if (lookaheadSymbol.ParentSymbol.Type != SymbolType.Nonterminal)
        {
          lookaheadSymbol.Configs.Add(new LRConfigTrack(config_, GetConfigTrackSourceModeForSymbol(checkahead_symbol)));
          lookaheadSymbolSet.Add(lookaheadSymbol);
        }
      }

      if (!checkahead_symbol.Nullable)
      {
        b_union_with_config_lookahead_set_flag = false;
        break;
      }      
    }
    if (b_union_with_config_lookahead_set_flag)
      lookaheadSymbolSet.UnionWith(config_.LookaheadSet);

    return lookaheadSymbolSet;
  }

  private static bool PopulateLookahead(LRConfig lr_config_)
  {
    if(!lr_config_.InheritLookahead)
      return false;

    for (int checkahead_offset = 0; checkahead_offset < lr_config_.CheckaheadCount(); ++checkahead_offset)
    {
      if(!lr_config_.Checkahead(checkahead_offset).Nullable)
        return false;
    }

    return true;
  }  
}
