//
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;


//
namespace gpp.builder;


//TODO  Почистить!
#nullable disable



internal sealed class BuilderFA
{
  private BuilderFAStatesList     _NFA;
  private int                     m_RootNFAState;   //setup in Build -> SetupForNFA
  //TODO  Странно, но оказалась локальной переменной в Build. Как?
  //private /*static*/ short            m_DFAStartState;  //setup in Build -> Build_BuildingDFA_Part_1
  //
  private AppNotify               m_app_notify;
  private AppLog                  m_app_log;
  private UnicodeTable            m_unicode_table;
  private BuilderSymbolsList      m_Symbols;
  private BuilderFACharsetsList   m_Charsets;
  private BuilderFAStatesList     m_DFA;
  private AppLog                _LOG      => m_app_log;
  private AppNotify             _NOTIFY   => m_app_notify;
  BuilderSymbolsList            _SYMBOLS  => m_Symbols;
  private BuilderFACharsetsList _CHARSETS => m_Charsets;
  private BuilderFAStatesList   _DFA      => m_DFA;



  private int AddDFAState(BuilderFAState fa_state_)
  {
    table_index_t fa_state_table_index = _DFA.Add(fa_state_);
    fa_state_.SetTableIndex(fa_state_table_index);
    return fa_state_table_index;
  }

  private int AddNFAState()
  {
    return this._NFA.Add(new BuilderFAState());
  }

  private bool CheckErrorsDFA(short dfa_start_state_)
  {
    bool b_has_dfa_error = false;

    if (!b_has_dfa_error && _DFA[(int)dfa_start_state_].AcceptList.Count() > 0)
    {
      for (int accept_index = 0; accept_index < _DFA[(int)dfa_start_state_].AcceptList.Count(); ++accept_index)
      {
        _LOG.Add(AppLogSection.DFA, AppLogAlert.Critical, "The terminal '"
          + _SYMBOLS[(int)_DFA[(int)dfa_start_state_].AcceptList[(int)accept_index].SymbolIndex].Name
          + "' can be zero length", "The definition of this terminal allows it to contain no characters.");
      }
      b_has_dfa_error = true;
    }

    if (!b_has_dfa_error)
    {
      for (int dfa_state_Index = 0; dfa_state_Index < _DFA.Count; ++dfa_state_Index)
      {
        BuilderFAState fa_state = _DFA[(int)dfa_state_Index];
        if (fa_state.AcceptList.Count() >= 2)
        {
          string s_title = "DFA new_fa_state " + dfa_state_Index + ": Cannot distinguish between: ";
          for (int accept_index2 = 0; accept_index2 < fa_state.AcceptList.Count(); ++accept_index2)
            s_title += _SYMBOLS[(int)fa_state.AcceptList[(int)accept_index2].SymbolIndex].Name + " ";
          //
          _LOG.Add(AppLogSection.DFA, AppLogAlert.Critical, s_title, BuilderFA.ConflictDesc(), dfa_state_Index.ToString());
          b_has_dfa_error = true;
        }
      }
    }

    if (!b_has_dfa_error)
    {
      //TODO  Это "разрушительная" проверка.
      //      Мы сначала сбрасываем у всех симболов .Accepted,
      //      а потом устанавливаем только у тех, которые используются в .DFA и имеют не пустой .AcceptList.
      //      В результате на выходе у нас ИЗМЕНЯЮТСЯ таблицы
      //      НУЖНО изменить алгоритм и начинать с как-то цикла по fa_state проверяя symbol.UsesDFA && symbol.Accepted
      //      при этом устанавливая в симболе флаг "проверено", а потом прокрутить цикл по симболам, которые "не-проверено"
      //      как-то так..
      for (int symbol_index = 0; symbol_index < _SYMBOLS.Count(); ++symbol_index)
        _SYMBOLS[(int)symbol_index].Accepted = false;

      for (int fa_state_index = 0; fa_state_index < _DFA.Count; ++fa_state_index)
      {
        BuilderFAState fa_state = _DFA[fa_state_index];
        if (fa_state.AcceptList.Count() != 0)
          _SYMBOLS[(int)fa_state.AcceptList[0].SymbolIndex].Accepted = true;
      }

      for (int symbol_index = 0; symbol_index < _SYMBOLS.Count(); ++symbol_index)
      {
        BuilderSymbol symbol = _SYMBOLS[(int)symbol_index];
        if (symbol.UsesDFA && !symbol.Accepted )
        {
          _LOG.Add(AppLogSection.DFA, AppLogAlert.Critical, "Unaccepted terminal: " + symbol.Name, "The terminal " + symbol.Name + " cannot be accepted by the DFA.");
          b_has_dfa_error = true;
        }
      }
    }
    
    return b_has_dfa_error;
  }


  private static string ConflictDesc()
  {
    return "The conflict is caused when two or more terminal definitions can accept the same regexp_item_data_text.";
  }


  public void CreateNFAStates(BuilderSymbol symbol_)
  {
    int nfa_state         = this.AddNFAState();
    int nfa_state_target  = this.AddNFAState();
    for (int reg_expr_index = 0; reg_expr_index < symbol_.RegularExp.Count(); ++reg_expr_index)
    {
      TerminalExpressionSequence    regexp_seq            = symbol_.RegularExp[(int)reg_expr_index];
      BuilderFA.AutomataNode        regexp_seq_automata   = this.CreateAutomataSeq(regexp_seq);
      this._NFA[nfa_state].CreateNewEdgeLambda(regexp_seq_automata.Head);
      this._NFA[regexp_seq_automata.Tail].CreateNewEdgeLambda(nfa_state_target);
      this._NFA[regexp_seq_automata.Tail].AcceptList.AddNewFAAccept(to_short(symbol_.TableIndex), regexp_seq.Priority);
    }
    this._NFA[this.m_RootNFAState].CreateNewEdgeLambda((int) nfa_state);
  }

  

  void Build_BuildingNFA()
  {
    _NOTIFY.Mode = AppProgramMode.BuildingNFA;
    //
    for (int symbol_index = 0; symbol_index < _SYMBOLS.Count(); ++symbol_index)
    {
      BuilderSymbol symbol = _SYMBOLS[symbol_index];
      if (symbol.UsesDFA)
      {
        _NOTIFY.Text = symbol.Name;
        this.CreateNFAStates(symbol);
      }
    }
  }


  void Build_NFAClosure() 
  {
    _NOTIFY.Mode = AppProgramMode.NFAClosure;
    //
    for (int nfa_index = 0; nfa_index < this._NFA.Count; ++nfa_index)
    {
      BuilderFAStatesIndexSet reachable_nfa_indexes = new();
      reachable_nfa_indexes.Add(nfa_index);
      this.CalculateClosureNFA(reachable_nfa_indexes);
      this._NFA[nfa_index].NFAClosure = reachable_nfa_indexes;
    }
  }


  short Build_BuildingDFA_Part_0_DFAStartState()
  {
    BuilderFAStatesIndexSet NFAList = new();
    NFAList.Add((int)this.m_RootNFAState);
    return this.BuildDFAState(NFAList);
  }

  // Здесь у нас на входе у каждого edge просто созданные чарсеты, возможно много едентичных, и мы в таблицу построителя заносим только уникальные
  // и обновляем edge_.Characters
  //TODO  То есть на этот момент собственно DFA уже построен и мы так сказать "марафет" наводим 
  //      ну и к сохранению и UI подготавливаемся
  //      Вполне может быть в отдельном потоке
  void Build_BuildingDFA_Part_1_SetUniqueCharsetsForDFAEdges()
  {
    for (int dfa_index = 0; dfa_index < _DFA.Count; ++dfa_index)
    {
      for (int dfa_edge_index = 0; dfa_edge_index < _DFA[dfa_index].Edges().Count(); ++dfa_edge_index)
        this.BuilderTablesCharsets_AddOrSetUniqueCharsetForFAEdgeBuild(_DFA[dfa_index].Edges()[dfa_edge_index]);
    }
  }
  //
  private void BuilderTablesCharsets_AddOrSetUniqueCharsetForFAEdgeBuild(BuilderFAEdge edge_)
  {
    for (int charset_index = 0; charset_index < _CHARSETS.Count(); ++charset_index)
    {
      if (_CHARSETS[charset_index].IsEqualSet(edge_.Characters))
      {
        Debug.Assert(_CHARSETS[charset_index].TableIndex == charset_index);
        edge_.SetCharacters(_CHARSETS[charset_index]);
        return;
      }
    }

    _CHARSETS.AddAndSetTableIndex(edge_.Characters);
  }
  


  //TODO  Этот метод может быть статик - он работает только с BuilderApp.BuildTables.DFA и Symbol
  //      их нужно передавать параметрами
  void Build_BuildingDFA_Part_2_SetAcceptSymbol()
  {    
    for (int dfa_state_index = 0; dfa_state_index < _DFA.Count; ++dfa_state_index)
    {
      BuilderFAState dfa_state = _DFA[dfa_state_index];
      if (dfa_state.AcceptList.Count() == 0)
        dfa_state.SetAcceptSymbol(null);
      else if (dfa_state.AcceptList.Count() == 1)
        dfa_state.SetAcceptSymbol(_SYMBOLS[dfa_state.AcceptList[0].SymbolIndex]); 
      /*      TODO  в качестве оптимизации возможно, если в списке два элемента, то сравнить их приоритеты и удалить элемент с низшим приоритетом.
       *      else if (dfa_state.AcceptList.Count() == 2) { 
              if(dfa_state.AcceptList[0].Priority < dfa_state.AcceptList[1].Priority)
                dfa_state.AcceptList.remove
            }
      */
      else
      {
        BuilderFAAcceptSymbolset fa_accept_symbols = new();
        BuilderFAAccept          fa_accept_1st     = dfa_state.AcceptList[0];
        fa_accept_symbols.Add((int)fa_accept_1st.SymbolIndex);
        short priority_prev = fa_accept_1st.Priority;
        //TODO  Не совсем понятно что-тут делают. Вроде как пытаются пересортировать AcceptList в соответствии с .Priority ??
        //      Но нет.. при нахождении элемента с более НИЗКИМ (высоким?) приоритетом весь список очищается!
        //      т.е. в конце в списке останется только набор элементов с самым-самым низким (высоким?) приоритетом,
        //      а все более низкоприоритетные из списка будут удалены.
        for (int dfa_state_accept_index = 1; dfa_state_accept_index < dfa_state.AcceptList.Count(); ++dfa_state_accept_index)
        {
          BuilderFAAccept fa_accept_this = dfa_state.AcceptList[dfa_state_accept_index];
          if (fa_accept_this.Priority == priority_prev)
            fa_accept_symbols.Add((int)fa_accept_this.SymbolIndex);
          else if (fa_accept_this.Priority < priority_prev)
          {
            fa_accept_symbols.Clear();
            fa_accept_symbols.Add((int)fa_accept_this.SymbolIndex);
            priority_prev = fa_accept_this.Priority;
          }
        }

        dfa_state.AcceptList.Clear();

        for (int i = 0; i < fa_accept_symbols.Count(); ++i)
          dfa_state.AcceptList.AddNewFAAccept(checked((short)fa_accept_symbols[i]), priority_prev);

        if (fa_accept_symbols.Count() == 1)
          dfa_state.SetAcceptSymbol(_SYMBOLS[fa_accept_symbols[0]]);
      }
    }
  }




  public static (bool, BuilderFACharsetsList, BuilderFAStatesList) DoBuild(AppNotify app_notify_, AppLog app_log_, 
                                                                            UnicodeTable unicode_table_,
                                                                            BuilderSymbolsList symbols_, bool case_sensitive_, CharMappingMode char_mapping_mode_)
  {
    BuilderFA builder_fa_engine = new BuilderFA(app_notify_, app_log_, unicode_table_, symbols_);
    return builder_fa_engine.Build(case_sensitive_, char_mapping_mode_);
  }
  private BuilderFA(AppNotify app_notify_, AppLog app_log_, UnicodeTable unicode_table_, BuilderSymbolsList symbols_)
  { 
    m_app_notify            = app_notify_;
    m_app_log               = app_log_;
    m_unicode_table         = unicode_table_;
    m_Symbols               = symbols_;
    //
    m_Charsets              = new BuilderFACharsetsList();
    m_DFA                   = new BuilderFAStatesList();
    //
    this._NFA               = new BuilderFAStatesList();
    this.m_RootNFAState     = this.AddNFAState();
  }
  private (bool, BuilderFACharsetsList, BuilderFAStatesList) Build(bool case_sensitive_, CharMappingMode char_mapping_mode_)
  {
    // В логе могут быть чужие критические Ош.
    size_t count_critical_at_begin = _LOG.AlertCount(AppLogAlert.Critical);    
    //    
    Build_BuildingNFA();
    Build_NFAClosure();

    if (this._NFA.Count > 0) 
    {              
      _LOG.Add(AppLogSection.DFA, AppLogAlert.Detail, "The initial Nondeterministic Finite Automata has " + this._NFA.Count + " states");

      _NOTIFY.Mode = AppProgramMode.NFACase;
      this.SetupMapCaseCharTables(case_sensitive_, char_mapping_mode_);

      _NOTIFY.Mode          = AppProgramMode.BuildingDFA;
      var dfa_start_state   = Build_BuildingDFA_Part_0_DFAStartState();
      Build_BuildingDFA_Part_1_SetUniqueCharsetsForDFAEdges();
      Build_BuildingDFA_Part_2_SetAcceptSymbol();

      //TODO  ранее, в оригинале, результат .CheckErrorsDFA можно было понять по наличию .Critical в логе
      //      также она не просто проверяет, но и переустанавливает Symbol.Accepted - ПЛОХО это.
      bool b_dfa_has_error = this.CheckErrorsDFA(dfa_start_state);
      if(!b_dfa_has_error)
        _LOG.Add(AppLogSection.DFA, AppLogAlert.Success, "The DFA new_fa_state Table was successfully created", "The table contains a total of " + this._DFA.Count + " states");
    }
    else
      _LOG.Add(AppLogSection.DFA, AppLogAlert.Critical, "There are no terminals in the grammar");

    return ((_LOG.AlertCount(AppLogAlert.Critical) == count_critical_at_begin), _CHARSETS, _DFA);
  }



  //TODO  Этот метод изначально вызывается с NumberSet содержащим один элемент
  //      а потом рекурсивно вызывает себя добавляя что-то в этот NumberSet
  //      Это не прозрачно. Нужно разбить на два метода - начальный, который принимает один элемент
  //      плюс рекурсия, которая заполняет результат. Вернуть результат
  //      (а лучше бы вообще уйти от рекурсии..)
  //TODO  Также надо подумать - а нужен ли здесь NumberSet или можно хэшсет ?
  private void CalculateClosureNFA(BuilderFAStatesIndexSet in_out_reachable_nfa_states_indexes_)
  {
    for (int i = 0; i < in_out_reachable_nfa_states_indexes_.Count(); ++i)
    { 
      int reachable_nfa_state_index = in_out_reachable_nfa_states_indexes_[i];
      for(int reachable_nfa_state_edge_index = 0; reachable_nfa_state_edge_index < this._NFA[reachable_nfa_state_index].Edges().Count(); ++reachable_nfa_state_edge_index)
      {
        int target = this._NFA[reachable_nfa_state_index].Edges()[reachable_nfa_state_edge_index].TargetFAStateIndex;
        if (this._NFA[reachable_nfa_state_index].Edges()[reachable_nfa_state_edge_index].Characters.IsEmpty() && !in_out_reachable_nfa_states_indexes_.Contains(target))
        {
          in_out_reachable_nfa_states_indexes_.Add(target);
          this.CalculateClosureNFA(in_out_reachable_nfa_states_indexes_);
        }
      }
    }
  }


  private BuilderFA.AutomataNode CreateAutomataSeq(TerminalExpressionSequence regexp_seq_)
  {
    int head = 0;
    int tail = 0;
    for(int regexp_index = 0; regexp_index <  regexp_seq_.Count(); ++regexp_index)
    {
      TerminalExpressionItem regexp_seq_item           = regexp_seq_[regexp_index];
      BuilderFA.AutomataNode regexp_seq_item_automata  = this.CreateAutomataItem(regexp_seq_item);
      if (regexp_index == 0)
      {
        head = regexp_seq_item_automata.Head;
        tail = regexp_seq_item_automata.Tail;
      }
      else
      {
        this._NFA[tail].CreateNewEdgeLambda(regexp_seq_item_automata.Head);
        tail = regexp_seq_item_automata.Tail;
      }
    }

    BuilderFA.AutomataNode regexp_seq_automata;
    regexp_seq_automata.Head = head;
    regexp_seq_automata.Tail = tail;
    return regexp_seq_automata;
  }


  private BuilderFA.AutomataNode CreateAutomataItem(TerminalExpressionItem regexp_item_)
  {
    int nfa_source_state_index      = 0;
    int nfa_target_state_index      = 0;
    int last_nfa_source_state_index = 0;
    int last_nfa_target_state_index = 0;

    bool regexp_item_data_type_valid = (regexp_item_.Data is TerminalExpression) || (regexp_item_.Data is CharsetExpressionItem);
    Debug.Assert(regexp_item_data_type_valid);
    if (!regexp_item_data_type_valid)
      throw BuilderError.Internal($"Invalid TerminalExpressionItem.Data type at CreateAutomataItem(): {regexp_item_.Data.GetType().Name}");

    if (regexp_item_.Data is TerminalExpression)
    {
      //TODO  А почему мы создаем объект только для того чтобы получить его .HeadSymbol и .Tail, а сам объект в мусорку ???
      //      ! ПОТОМУ что там сайд-эффект добавления .CreateNewEdgeLambda, а сам .AutomataType это просто структура, не объект
      AutomataNode sub_automata   = this.CreateSubAutomata((TerminalExpression)regexp_item_.Data);            
      nfa_source_state_index      = (int)sub_automata.Head;
      nfa_target_state_index      = (int)sub_automata.Tail;
      last_nfa_source_state_index = nfa_source_state_index;
      last_nfa_target_state_index = nfa_target_state_index;
    }
    else if (regexp_item_.Data is CharsetExpressionItem)
    {
      CharsetExpressionItem regexp_item_data = (CharsetExpressionItem) regexp_item_.Data;

      switch (regexp_item_data.Type)
      {
        case CharsetExpressionItemType.Chars:
          {
            BuilderFACharset characters   = (BuilderFACharset)regexp_item_data.Characters;
            nfa_source_state_index        = this.AddNFAState();
            nfa_target_state_index        = this.AddNFAState();
            this._NFA[nfa_source_state_index].CreateNewEdge(characters, nfa_target_state_index);
            last_nfa_source_state_index   = nfa_source_state_index;
            last_nfa_target_state_index   = nfa_target_state_index;
          }
          break;
        case CharsetExpressionItemType.Name:
          {
            Debug.Assert(regexp_item_data.Characters != null);      // мы это теперь вычисляем при Populate
            BuilderFACharset named_charset  = regexp_item_data.Characters;
            nfa_target_state_index          = this.AddNFAState();
            nfa_source_state_index          = this.AddNFAState();
            this._NFA[nfa_source_state_index].CreateNewEdge(named_charset, nfa_target_state_index);
            last_nfa_source_state_index = nfa_source_state_index;
            last_nfa_target_state_index = nfa_target_state_index;
          }
          break;
        case CharsetExpressionItemType.Sequence:
          {
            nfa_source_state_index = this.AddNFAState();
            nfa_target_state_index = nfa_source_state_index;
            //TODO  Похоже что если .Text будет пустой, то получится какая-то ересь
            Debug.Assert(regexp_item_data.Text.Length > 0);
            int prev_nfa_target_state_index = 0;
            string regexp_item_data_text    = regexp_item_data.Text;
            for (int regexp_item_data_text_char_index = 0; regexp_item_data_text_char_index < regexp_item_data_text.Length; ++regexp_item_data_text_char_index)
            {
              prev_nfa_target_state_index = nfa_target_state_index;
              nfa_target_state_index      = this.AddNFAState();
              this._NFA[prev_nfa_target_state_index].CreateNewEdge(new BuilderFACharset(  regexp_item_data_text[regexp_item_data_text_char_index]  ), nfa_target_state_index);
            }
            last_nfa_source_state_index = prev_nfa_target_state_index;
            last_nfa_target_state_index = nfa_target_state_index;
          }
          break;
        default:
          throw BuilderError.Internal($"Invalid CharsetExpressionItemType at CreateAutomataItem(): {regexp_item_data.Type.GetType().Name}");
      }
    }
      
    //TODO  это в оригинале так. видимо здесь хотели сделать выход из метода
    //      ввиду того что выше какая-то ош, которую прописали в лог
    //      (тогда переменные без инициализации останутся)
    //      НО! из метода то мы должны выйти что-то вернув...
    if (last_nfa_source_state_index == 0 || last_nfa_target_state_index == 0)
      Debug.WriteLine("ERROR: BAD KLEENE DATA");

    KleeneOp k_kleene_op = regexp_item_.KleeneOp;
    if (k_kleene_op == KleeneOp.Zero_Or_More)
    {
      this._NFA[last_nfa_source_state_index].CreateNewEdgeLambda(last_nfa_target_state_index);
      this._NFA[last_nfa_target_state_index].CreateNewEdgeLambda(last_nfa_source_state_index);
    }
    else if (k_kleene_op == KleeneOp.One_Or_More)
      this._NFA[last_nfa_target_state_index].CreateNewEdgeLambda(last_nfa_source_state_index);
    else if (k_kleene_op == KleeneOp.Zero_Or_One)
      this._NFA[last_nfa_source_state_index].CreateNewEdgeLambda(last_nfa_target_state_index);

    BuilderFA.AutomataNode automataItem;
    automataItem.Head = checked ((short) nfa_source_state_index);
    automataItem.Tail = checked ((short) nfa_target_state_index);
    return automataItem;
  }

  private BuilderFA.AutomataNode CreateSubAutomata(TerminalExpression regexp_)
  {
    int nfa_source_index = this.AddNFAState();
    int nfa_target_index = this.AddNFAState();

    for (int regexp_index = 0; regexp_index < regexp_.Count(); ++regexp_index)
    {
      TerminalExpressionSequence  regexp_seq          = regexp_[regexp_index];
      BuilderFA.AutomataNode      regexp_seq_automata = this.CreateAutomataSeq(regexp_seq);
      this._NFA[nfa_source_index].CreateNewEdgeLambda(regexp_seq_automata.Head);
      this._NFA[regexp_seq_automata.Tail].CreateNewEdgeLambda(nfa_target_index);
    }

    BuilderFA.AutomataNode subAutomata;
    subAutomata.Head = nfa_source_index;
    subAutomata.Tail = nfa_target_index;
    return subAutomata;
  }


  private short BuildDFAState(BuilderFAStatesIndexSet nfa_set_)
  {
    int result_fa_state_index;

    BuilderFAState new_fa_state = new BuilderFAState();    
    //
    for (int nfa_index = 0; nfa_index < nfa_set_.Count(); ++nfa_index)
      new_fa_state.NFAStates.UnionWith(this._NFA[nfa_set_[nfa_index]].NFAClosure);
    
    short fa_state_existing_index = this.DFAStateIndexExisting(new_fa_state);
    //
    if (fa_state_existing_index == (short) -1)
    {
      ++_NOTIFY.Counter; 
      int new_fa_state_table_index = this.AddDFAState(new_fa_state);

      for (int fa_state_index = 0; fa_state_index < new_fa_state.NFAStates.Count(); ++fa_state_index)
      {
        BuilderFAState nfa_state = this._NFA[new_fa_state.NFAStates[fa_state_index]];
        for (int fa_accept_index = 0; fa_accept_index < nfa_state.AcceptList.Count(); ++fa_accept_index)
        {
          BuilderFAAccept accept = nfa_state.AcceptList[fa_accept_index];
          new_fa_state.AcceptList.Add(accept);
        }
      }
      
      BuilderFAEdgesList       new_fa_edges_list             = new BuilderFAEdgesList();
      //TODO  Вроде бы этот new_fa_edges_list_targets_set используется просто как набор уникальных значений
      //      кандидат на хэшсет
      BuilderFAStatesIndexSet  new_fa_edges_list_targets_set = new ();

      for (int nfa_state_index = 0; nfa_state_index < new_fa_state.NFAStates.Count(); ++nfa_state_index)
      {
        BuilderFAState nfa_state = this._NFA[new_fa_state.NFAStates[nfa_state_index]];
        for (int edge_index = 0; edge_index < nfa_state.Edges().Count(); ++edge_index)
        {
          if (!nfa_state.Edges()[edge_index].Characters.IsEmpty())
          {
            new_fa_edges_list.AddFromBuildDFAState(nfa_state.Edges()[edge_index]);
            new_fa_edges_list_targets_set.Add(nfa_state.Edges()[edge_index].TargetFAStateIndex);
          }
        }
      }

      if (new_fa_edges_list_targets_set.Count() >= 1)
      {
        BuilderFACharset[] new_charsets_array = new BuilderFACharset[new_fa_edges_list_targets_set.Count()];

        for (int new_target_index = 0; new_target_index < new_fa_edges_list_targets_set.Count(); ++new_target_index)
        {
          checked { ++_NOTIFY.Analyzed; }

          int new_target                = new_fa_edges_list_targets_set[new_target_index];
          BuilderFACharset new_charset  = new BuilderFACharset();         
          int new_fa_edges_list_count   = new_fa_edges_list.Count();

          //  Эти два цикла нельзя совмещать в один - "минусовать" мы должны только после того, как все добавили
          //  (это не математики - "минус символа" не существует
          for (int edge_index = 0; edge_index < new_fa_edges_list_count; ++edge_index)
          {
            BuilderFAEdge fa_edge = (BuilderFAEdge)new_fa_edges_list[edge_index];
            if (fa_edge.TargetFAStateIndex == (int)new_target)
              new_charset.SetUnionWith(fa_edge.Characters);
          }
          for (int edge_index = 0; edge_index < new_fa_edges_list_count; ++edge_index)
          {
            BuilderFAEdge fa_edge = (BuilderFAEdge)new_fa_edges_list[edge_index];
            if (fa_edge.TargetFAStateIndex != (int)new_target)
              new_charset.SetDifferenceWith(fa_edge.Characters);
          }

          new_charsets_array[new_target_index] = new_charset;
        }

        //TODO  Этот чарсет-времянка создается только для того, чтобы ниже перебрать его по одному чару
        //      т.е. по сути это какой-то иной спец-контейнер. 
        //      НО все же нужны эффективные .UnionWith и .DifferenceWith (это .Remove)
        BuilderFACharset tmp_charset = new BuilderFACharset();

        for (int edge_index = 0; edge_index < new_fa_edges_list.Count(); ++edge_index)
          tmp_charset.SetUnionWith(new_fa_edges_list[edge_index].Characters);

        int new_fa_edges_list_targets_set_count = new_fa_edges_list_targets_set.Count();

        for (int target_index = 0; target_index < new_fa_edges_list_targets_set_count; ++target_index)
          tmp_charset.SetDifferenceWith(new_charsets_array[target_index]);

        for (int new_charset_index = 0; new_charset_index < new_fa_edges_list_targets_set_count; ++new_charset_index)
        {
          if (!new_charsets_array[new_charset_index].IsEmpty() )
          {
            BuilderFAStatesIndexSet NFAList1 = new();
            NFAList1.Add(new_fa_edges_list_targets_set[new_charset_index]);
             ++_NOTIFY.Analyzed;
            //TODO  мы вызываем себя же с NumberSet-ом в который положили всего один таргет ???  
            //      Гхм.. может как-то быть проще надо?
            //      рекурсия хере, однако..
            new_fa_state.CreateNewEdge(new_charsets_array[new_charset_index], (int)this.BuildDFAState(NFAList1));   
          }
        }

        //Debug.WriteLine($"tmp_charset.Count = {tmp_charset.Count()}");
        // мы здесь где-то 391 раз, случаях в 10 размер порядка 350-360, случаях в 50 размер 0, в остальных размер 2-3, 10-12
        //  ПС. собственно Debug.WriteLine на скорость практически не повлиял
        foreach (int one_char_in_charset in tmp_charset)
        {
          BuilderFAStatesIndexSet NFAList1 = new();
          for (int fa_edge_index = 0; fa_edge_index < new_fa_edges_list.Count(); ++fa_edge_index)
          {
            BuilderFAEdge edge = (BuilderFAEdge)new_fa_edges_list[fa_edge_index];
            if (edge.Characters.Contains(one_char_in_charset))
              NFAList1.Add(edge.TargetFAStateIndex);
          }
          if (NFAList1.Count() >= 1)
          {
            //TODO  Странно как-то создавать целый чарсет для всего одной чар ?
            BuilderFACharset new_charset = new BuilderFACharset();
            new_charset.Add(one_char_in_charset);
            ++_NOTIFY.Analyzed;
            new_fa_state.CreateNewEdge(new_charset, (int)this.BuildDFAState(NFAList1));
          }
        }
      }
      result_fa_state_index = new_fa_state_table_index;
    }
    else
      result_fa_state_index = fa_state_existing_index;

    return to_short(result_fa_state_index);
  }


  private short DFAStateIndexExisting(BuilderFAState fa_state_)
  {
    for (int fa_state_index = 0; fa_state_index < _DFA.Count; ++fa_state_index)
    {
      if (_DFA[fa_state_index].NFAStates.IsEqualSet(fa_state_.NFAStates))
        return (short)fa_state_index;
    }
    return -1;
  }



  private void SetupMapCaseCharTables(bool CaseSensitive, CharMappingMode Mapping)
  {
    int nfa_count = this._NFA.Count;

    if (!CaseSensitive)
    {
      for (int nfa_index = 0; nfa_index < nfa_count; ++nfa_index)
        this._NFA[nfa_index].PerformCaseClosureForEdges(m_unicode_table);
    }

    if (Mapping != CharMappingMode.Windows1252)
      return;

    for(int nfa_index = 0; nfa_index < nfa_count; ++nfa_index)
      this._NFA[nfa_index].PerformMappingClosureForEdges(m_unicode_table /*, Mapping*/); // в методе не используется. видимо "на-вырост" туда передавалось
  }

  private struct AutomataNode
  {
    public int Head;
    public int Tail;
  }
}
