//
using System.Diagnostics;
using static SimpleDB;


//
//
namespace gpp.builder;


internal static class BuilderDbLoader
{
  private class BuilderTablesViewLoader : BuilderTablesView
  {
    public BuilderTablesViewLoader(BuilderPropertiesList properties_, BuilderSymbolsList symbols_, BuilderGroupsList groups_, BuilderProductionsList productions_,
      BuilderLRStatesList lr_, BuilderFAStatesList fa_, BuilderFACharsetsList charsets_)
      : base(properties_, symbols_, groups_, productions_, lr_, fa_, charsets_)
    { }
  }
  private class BuilderSymbolLoader : BuilderSymbol
  {
    internal BuilderSymbolLoader(string name_, SymbolType type_, table_index_t table_index_)
      //TODO  created_by_: SymbolCreationType.Defined здесь неправильно. По хорошему нужен BuilderSymbolView
      : base(MakeIdentificator(name_), name_, type_, UsesDFA_: BuilderSymbol.ImpliedDFAUsage(type_), created_by_: SymbolCreationType.Defined, table_index_, regexp_or_null_: null)
    { }
  }
  private class SymbolBuildListLoader(int size_, BuilderSymbol initial_symb_end_, BuilderSymbol initial_symb_err_) : BuilderSymbolsList(size_, initial_symb_end_, initial_symb_err_)
  {
    public void AddFromLoader(BuilderSymbol symb_)
    {
      Debug.Assert(symb_.Type != SymbolType.Error && symb_.Type != SymbolType.End);
      Debug.Assert(symb_.TableIndex == base.m_data.Count);
      _internal_add(symb_);
    }
  }
  private class BuilderGroupLoader(table_index_t table_index_, string name_, BuilderSymbol container_, BuilderSymbol start_symb_, BuilderSymbol end_symb_, GroupAdvanceMode advance_mode_, GroupEndingMode ending_mode_)
    : BuilderGroup(table_index_, name_, container_, start_symb_, end_symb_, advance_mode_, ending_mode_)
  { }
  private class BuilderCharsetsListLoader(int size_) : BuilderFACharsetsList(size_)
  {
    public void AddFromLoader(BuilderFACharset item_)
    {
      Debug.Assert(item_.TableIndex == base.m_data.Count);
      base.m_data.Add(item_);
    }
  }
  private class BuilderProductionLoader(BuilderSymbol head_symbol_, short table_index_, BuilderProductionHandleSymbolsetLoader handle_symbols_)
  : BuilderProduction(head_symbol_, table_index_, handle_symbols_)
  { }
  private class BuilderProductionsListLoader(size_t capacity_) : BuilderProductionsList(capacity_)
  {
    public void AddFromLoader(int index_, BuilderProduction item_)
    {
      //TODO  Это должен быть не ассерт, а рантайм еррор. Или, возможно, это поведение должно настраиваться.
      Debug.Assert(index_ == m_data.Count);
      m_data.Add(item_);
    }
  }
  private class BuilderProductionHandleSymbolsetLoader : BuilderProductionHandleSymbolset
  {
    public void AddFromLoader(BuilderSymbol symbol_) => base.m_data.Add(symbol_);
  }

  private class BuilderFAStateLoader : BuilderFAState
  {
    public BuilderFAStateLoader(table_index_t table_index_, BuilderSymbol accept_symbol_)
      : base(table_index_, accept_symbol_)
    { }
    public BuilderFAStateLoader(table_index_t table_index_)
      : base(table_index_, accept_symbol_or_null_: null)
    { }
  }
  private class FAStateBuildListLoader(size_t size_) : BuilderFAStatesList(size_)
  {
    internal void AddFromLoaderAtIndex(int index_, BuilderFAState item_)
    {
      Debug.Assert(item_.TableIndex == base.m_data.Count);
      base.m_data.Add(item_);
    }
    public void SetInitialStateFromLoader(table_index_t initial_state_)
    {
      base.SetInitialState(initial_state_);
    }
  }
  private class LRStateBuildListLoader(size_t size_) : BuilderLRStatesList(size_)
  {
    internal void AddFromLoader(int index_, BuilderLRState state_)
    {
      Debug.Assert(index_ == base.m_data.Count);
      base.m_data.Add(state_);
    }
  }
  private class GroupBuildListLoader(int size_) : BuilderGroupsList(size_)
  {
    public void AddFromLoader(BuilderGroup group_)
    {
      Debug.Assert(group_.TableIndex == base.m_data.Count);
      base.m_data.Add(group_);
    }
  }


  public static BuilderTablesView? LoadBuilderTablesView(AppLog log_, string egt_file_path_name_)
  {
    try
    {
      return LoadBuilderTablesView(new SimpleDB.Reader(egt_file_path_name_));
    }
    catch (Exception e)
    {
      log_.Add(AppLogSection.System, AppLogAlert.Critical, "Exeption in BuilderTablesLoad", e.ToString());
      return null;
    }
  }

  public static BuilderTablesView LoadBuilderTablesView(SimpleDB.Reader egt_file_reader_)
  {
    string s_reader_header = egt_file_reader_.Header();

    if (s_reader_header == GppGlobal.s_GOLD_Parser_Tables_v1_0)
      return BuilderDbLoader.BuilderTableLoadVer1(egt_file_reader_);
    else if (s_reader_header == GppGlobal.s_GOLD_Parser_Tables_v5_0)
      return BuilderDbLoader.BuilderTableLoadVer_5_0(egt_file_reader_);
    else
      throw new ArgumentException("Invalid egt file header: " + s_reader_header);
  }



  public static BuilderTablesView BuilderTableLoadVer_5_0(SimpleDB.Reader rgt_reader_)
  {
    // 1 Properties
    // 2 EGTRecord.TableCounts

    BuilderPropertiesList this_Properties = new BuilderPropertiesList();

    while (!rgt_reader_.EndOfFile())
    {
      rgt_reader_.GetNextRecord();
      EGTRecord egt_record_code = (EGTRecord)rgt_reader_.RetrieveByte();
      switch (egt_record_code)
      {
        case EGTRecord.Property:
        { // Свойства сохраняются в порядке EGTProperty. Первый элемент это EGTProperty
          int property_id = rgt_reader_.RetrieveInt16();
          this_Properties.AddFromLoader(property_id, rgt_reader_.RetrieveString(), rgt_reader_.RetrieveString());
        }
        break;
        case EGTRecord.TableCounts:
        {
          SymbolBuildListLoader         this_Symbol      = new SymbolBuildListLoader(rgt_reader_.RetrieveInt16(), BuilderSymbol.CreateDefaultSymbol_End(), BuilderSymbol.CreateDefaultSymbol_Err());
          BuilderCharsetsListLoader     this_Charsets    = new BuilderCharsetsListLoader(rgt_reader_.RetrieveInt16());
          BuilderProductionsListLoader  this_Production  = new BuilderProductionsListLoader(rgt_reader_.RetrieveInt16());
          FAStateBuildListLoader        this_DFA         = new FAStateBuildListLoader(rgt_reader_.RetrieveInt16());
          LRStateBuildListLoader        this_LALR        = new LRStateBuildListLoader(rgt_reader_.RetrieveInt16());
          GroupBuildListLoader          this_Group       = new GroupBuildListLoader(rgt_reader_.RetrieveInt16());

          return BuilderTableLoadVer_5_0_Tail(rgt_reader_, this_Properties, this_Symbol, this_Charsets, this_Production, this_DFA, this_LALR, this_Group);
        }
        default:
          throw new Exception("File Error. A record of type '" + vb_compatable_ChrW((int)egt_record_code).ToString() + "' was read in egt-file head. this is not a valid code.");
      }
    }
    throw new Exception("File Error. Unexpected egt_reader_.EndOfFile().");
  }

  private class BuilderFACharsetLoader(table_index_t table_index_) : BuilderFACharset(table_index_)
  {
    public void AddRangeFromEgt(int ch_begin_, int ch_final_)
    {
      if(is_valid_range(ch_begin_, ch_final_))
        base.AddRangeFromLoader(ch_begin_, ch_final_);
      else
        throw new Exception("File contains invalid range.");      
    }
    public void AddRangeFromEgtNext(ref int in_out_char_final_prev_, int ch_begin_, int ch_final_)
    {
      if (ch_begin_ > in_out_char_final_prev_)
      {
        AddRangeFromEgt(ch_begin_, ch_final_);
        in_out_char_final_prev_ = ch_final_;
      }
      else
        throw new Exception("File contains invalid ranges sequence.");
    }
  }

  private static BuilderTablesView BuilderTableLoadVer_5_0_Tail(SimpleDB.Reader egt_reader_, BuilderPropertiesList this_Properties, SymbolBuildListLoader this_Symbol, 
    BuilderCharsetsListLoader this_Charsets, BuilderProductionsListLoader this_Production, FAStateBuildListLoader this_DFA, LRStateBuildListLoader this_LALR, GroupBuildListLoader this_Group)
  {
    while (!egt_reader_.EndOfFile())
    {
      egt_reader_.GetNextRecord();
      EGTRecord egt_record_code = (EGTRecord)egt_reader_.RetrieveByte();
      switch (egt_record_code)
      {
        case EGTRecord.InitialStates:
        {
          ((FAStateBuildListLoader)this_DFA).SetInitialStateFromLoader(egt_reader_.RetrieveInt16());
          this_LALR.InitialState = checked((short)egt_reader_.RetrieveInt16());
        }
        break;
        case EGTRecord.CharRanges:
        {              
          int charset_table_index         = egt_reader_.RetrieveInt16();
          egt_reader_.RetrieveInt16();  //.StoreInt16(0);
          int c_ranges                    = egt_reader_.RetrieveInt16();
          egt_reader_.RetrieveEntry();  //.StoreEmpty()
          BuilderFACharsetLoader charset  = new BuilderFACharsetLoader(charset_table_index);

          if (c_ranges > 0)
          {
            char_t ch_begin = egt_reader_.RetrieveInt16();
            char_t ch_final = egt_reader_.RetrieveInt16();
            charset.AddRangeFromEgt(ch_begin, ch_final);
            for (size_t i_range = 1; i_range < c_ranges; ++i_range)
              charset.AddRangeFromEgtNext(ref ch_final, egt_reader_.RetrieveInt16(), egt_reader_.RetrieveInt16());                
          }
          if (!egt_reader_.RecordComplete())
              throw new Exception("File structure corrupt.");

          ((BuilderCharsetsListLoader)this_Charsets).AddFromLoader(charset);
        }
        break;
        case EGTRecord.Symbol:
        {
          int         symb_index  = egt_reader_.RetrieveInt16();
          string      symb_name   = egt_reader_.RetrieveString();
          SymbolType  symb_type   = (SymbolType)egt_reader_.RetrieveInt16();
          if(symb_index == 0)
          { 
            Debug.Assert(symb_type == SymbolType.End);
            Debug.Assert(this_Symbol.Count() == 2);
            Debug.Assert(this_Symbol[0].Type == SymbolType.End);
            Debug.Assert(this_Symbol[0].Name == symb_name);
          }
          else if(symb_index == 1)
          {
            Debug.Assert(symb_type == SymbolType.Error);
            Debug.Assert(this_Symbol.Count() == 2);
            Debug.Assert(this_Symbol[1].Type == SymbolType.Error);
            Debug.Assert(this_Symbol[1].Name == symb_name);
          }
          else
            this_Symbol.AddFromLoader(new BuilderSymbolLoader(symb_name, symb_type, checked((short)symb_index)));
        }
        break;
        case EGTRecord.Group:
        {
          int               group_table_index = egt_reader_.RetrieveInt16();
          string            group_Name        = egt_reader_.RetrieveString();
          BuilderSymbol     group_Container   = this_Symbol[egt_reader_.RetrieveInt16()];
          BuilderSymbol     group_Start       = this_Symbol[egt_reader_.RetrieveInt16()];
          BuilderSymbol     group_End         = this_Symbol[egt_reader_.RetrieveInt16()];
          GroupAdvanceMode  group_Advance     = (GroupAdvanceMode)egt_reader_.RetrieveInt16();
          GroupEndingMode   group_Ending      = (GroupEndingMode)egt_reader_.RetrieveInt16();
          BuilderGroupLoader  group = new BuilderGroupLoader(group_table_index, group_Name, group_Container, group_Start, group_End, group_Advance, group_Ending);
          egt_reader_.RetrieveEntry();
          int group_nesting_index_max = egt_reader_.RetrieveInt16();
          //TODO  вот так в оригинале лоадера почему-то итерация идет от нижней границы 1 до записанной в файле верхней границы. видимо артефакт бейсика
          int group_nesting_index_min = 1;
          for (int group_nesting_index = group_nesting_index_min; group_nesting_index <= group_nesting_index_max; ++group_nesting_index)
            group.Nesting.Add(egt_reader_.RetrieveInt16());
          group.Container.SetGroupFromLoader(group);
          group.Start.SetGroupFromLoader(group);
          group.End.SetGroupFromLoader(group) ;
          //
          ((GroupBuildListLoader)this_Group).AddFromLoader(group);
        }
        break;
        case EGTRecord.Production:
        {
          int production_table_index        = egt_reader_.RetrieveInt16();
          int production_head_symbol_index  = egt_reader_.RetrieveInt16();
          egt_reader_.RetrieveEntry();              //egt_writer.StoreEmpty();  TODO                          
          BuilderProductionHandleSymbolsetLoader production_handle_symbols = new BuilderProductionHandleSymbolsetLoader();
          while (!egt_reader_.RecordComplete())
          {
            int production_handle_symbol_index = egt_reader_.RetrieveInt16();
            production_handle_symbols.AddFromLoader(this_Symbol[production_handle_symbol_index]);
          }
          BuilderProductionLoader production  = new BuilderProductionLoader((BuilderSymbol)this_Symbol[production_head_symbol_index], to_short(production_table_index), production_handle_symbols);
          ((BuilderProductionsListLoader)this_Production).AddFromLoader(production_table_index, production);
        }
        break;
        case EGTRecord.DFAState:
        { 
          int   fa_state_index        = egt_reader_.RetrieveInt16();
          bool  fa_state_accept       = egt_reader_.RetrieveBoolean();
          int   fa_state_accept_index = egt_reader_.RetrieveInt16();    //TODO  если !fa_state_accept, то это поле выбирать ненадо
          egt_reader_.RetrieveEntry();        //.StoreEmpty();
          ((FAStateBuildListLoader)this_DFA).AddFromLoaderAtIndex(fa_state_index, 
            (fa_state_accept? new BuilderFAStateLoader(fa_state_index, this_Symbol[fa_state_accept_index]) : new BuilderFAStateLoader(fa_state_index)));
          while (!egt_reader_.RecordComplete())
          {
            int charset_index           = egt_reader_.RetrieveInt16();
            int fa_target_state_index   = egt_reader_.RetrieveInt16();
            egt_reader_.RetrieveEntry();
            this_DFA[fa_state_index].Edges().AddFromLoader(new BuilderFAEdge((BuilderFACharset)this_Charsets[charset_index], fa_target_state_index));
          }
        } 
        break;
        case EGTRecord.LRState:
        {
          int lr_state_index = egt_reader_.RetrieveInt16();
          egt_reader_.RetrieveEntry();
          ((LRStateBuildListLoader)this_LALR).AddFromLoader(lr_state_index, new BuilderLRState());
          while (!egt_reader_.RecordComplete())
          {
            int lr_action_symbol_index  = egt_reader_.RetrieveInt16();
            int lr_action_act_type      = egt_reader_.RetrieveInt16();
            int lr_action_act_value     = egt_reader_.RetrieveInt16();
            egt_reader_.RetrieveEntry();
            this_LALR[lr_state_index].AddLRAction(new BuilderLRAction(this_Symbol[lr_action_symbol_index], (LRActionType)lr_action_act_type, checked((short)lr_action_act_value)));
          }
        }
        break;
        default:
          throw new Exception("File Error. A record of type '" + vb_compatable_ChrW((int)egt_record_code).ToString() + "' was read in egt-file tail. this is not a valid code.");
      }//switch (egt_record_code)
    }//while (!rgt_reader_.EndOfFile())

    return new BuilderTablesViewLoader(this_Properties, this_Symbol, this_Group, this_Production, this_LALR, this_DFA, this_Charsets);
  }//BuilderTableLoadVer_5_0_Tail



#if TODO_HERE
  //TODO  Пока просто отключил ..Ver1 чтобы под ногами не путалось.
  //      По хорошему лоадеров\сэйверов нужно делать отдельными классми и подключать\отключать особо
  public static BuilderTablesView BuilderTableLoadVer1(SimpleDB.Reader CGT)
  {
    Debug.Assert(false);
    throw new Exception("BuilderTableLoadVer1 Not Implemented Yet..");
  }
  //
#else
  //TODO  here
#endif
}
