//
using gpp.builder;
using System.Diagnostics;
//
//
namespace gpp.parser;



internal class ParserDbLoader
{

  private class ParserPropertiesLoader : ParserProperties
  {
    public ParserPropertiesLoader() : base()
    { }
    public void AddFromLoader(int property_id_, string name_, string value_)
    {
      //TODO  For well-known props property_id_ must be >=0. For user props must be <0.
      //      but we not have user props for while so simple check with assert
      Debug.Assert(property_id_ >= 0);

      if (property_id_ >= 0 && property_id_ <= PropertyExtension.EGTProperty_Max)
      {
        Debug.Assert(m_data[property_id_].Name == name_);
        base.m_data[property_id_].UpdateValue(value_);
      }
      else
        base.m_data.Add(new ParserProperties.ParserPropertyUpdatable(property_id_, name_, value_));
    }
  }

  private class CharacterSetLoader(table_index_t table_index_, size_t charset_ranges_count_) : ParserCharset(table_index_, charset_ranges_count_)
  {
    public void AddFromLoaderRange(ref size_t in_out_ranges_counter_, char_t range_begin_, char_t range_final_)
    {
      if(!is_valid_range(range_begin_, range_final_))
        throw new Exception("File contains invalid range.");
      if(in_out_ranges_counter_ >= base.m_charrangs.Length)
        throw new Exception("File contains too many ranges.");
      if (in_out_ranges_counter_ > 0 && (base.m_charrangs[in_out_ranges_counter_ -1 ].range_final >= range_begin_))
        throw new Exception("File contains invalid ranges sequence.");

      base.m_charrangs[in_out_ranges_counter_].range_begin = range_begin_;
      base.m_charrangs[in_out_ranges_counter_].range_final = range_final_;
      ++in_out_ranges_counter_;
    }
    public void AddFromLoaderRangesDone(size_t ranges_counter_)
    {
      if(ranges_counter_ != base.m_charrangs.Length)
        throw new Exception("File structure corrupt.");
    }
  }

  private class CharacterSetListLoader(size_t size_) : ParserCharsetsList(size_)
  {
    public void AddFromLoader(ParserCharset item_)
    {
      Debug.Assert(item_.TableIndex == base.m_data.Count);
      base.m_data.Add(item_);
    }
  }

  private class FAStateListLoader(size_t size_) : ParserFAStatesList(size_)
  {
    internal void AddFromLoaderAtIndex(int index_, ParserFAState item_)
    {
      Debug.Assert(index_ == base.m_data.Count);
      base.m_data.Add(item_);
    }
    public void SetInitialStateFromLoader(table_index_t initial_state_)
    {
      base.m_InitialState = initial_state_;
    }
  }

  private class GroupLoader(table_index_t table_index_, string name_, ParserSymbol container_, ParserSymbol start_symb_, ParserSymbol end_symb_, GroupAdvanceMode advance_mode_, GroupEndingMode ending_mode_) 
    : ParserGroup(table_index_, name_, container_, start_symb_, end_symb_, advance_mode_, ending_mode_)
  {
    public void AddNestingFromLoader(int value_)
    {
      base.m_NestingBmp.Set((uint)value_);
    }
  }

  private class GroupListLoader(size_t size_) : ParserGroupList(size_)
  {
    public void AddFromLoader(ParserGroup group_)
    {
      Debug.Assert(group_.TableIndex == base.m_data.Count);
      base.m_data.Add(group_);
    }
  }

  private class LRStateLoader() : ParserLRState()
  {
    public void AddLRActionFromLoader(ParserLRAction lr_action_)
    {
      base.m_data.Add(lr_action_);
    }
  }

  private class LRStateListLoader(size_t size_) : ParserLRStateList(size_)
  {
    internal void AddFromLoader(int lr_state_index_, ParserLRState state_)
    {
      Debug.Assert(lr_state_index_ == base.m_data.Count);
      m_data.Add(state_);
    }
    internal void SetInitialStateFromLoader(table_index_t initial_lr_state_index_)
    {
      base.m_InitialState = to_short(initial_lr_state_index_);
    }
  }

  private class ProductionLoader(ParserSymbol head_symbol_, short table_index_) : ParserProduction(head_symbol_, table_index_)
  {
    public new void AddHandleFromLoader(ParserSymbol handle_symbol_)
    {
      base.AddHandleFromLoader(handle_symbol_);
    }
  }

  private class ProductionListLoader(size_t capacity_) : ParserProductionList(capacity_)
  {
    public void AddFromLoader(int index_, ParserProduction item_)
    {
      //TODO  Это должен быть не ассерт, а рантайм еррор. Или, возможно, это поведение должно настраиваться.
      Debug.Assert(index_ == m_data.Count);
      m_data.Add(item_);
    }
  }

  private class SymbolLoader(string name_, SymbolType type_, short table_index_) : ParserSymbol(name_, type_, table_index_)
  {
    public void SetGroupFromLoader(ParserGroup group_)
    {
      base.m_Group = group_;
    }
  }

  private class SymbolListLoader(int capacity_) : ParserSymbolList(capacity_)
  {
    public void AddFromLoader(ParserSymbol item_)
    {
      Debug.Assert(item_.TableIndex == m_Array.Count);
      this.m_Array.Add(item_);
    }
    public void AddFromLoader_End(ParserSymbol item_)
    {
      Debug.Assert(item_.TableIndex == m_Array.Count);
      m_symbol_end = item_;
      this.m_Array.Add(item_);
    }
    public void AddFromLoader_Err(ParserSymbol item_)
    {
      Debug.Assert(item_.TableIndex == m_Array.Count);
      m_symbol_err = item_;
      this.m_Array.Add(item_);
    }
  }

  private class ParserTablesLoader : ParserTables
  {
    public ParserTablesLoader(ParserProperties properties_, ParserSymbolList symbols_, ParserGroupList groups_, ParserProductionList productions_,
                                ParserFAStatesList fa_, ParserCharsetsList charsets_, ParserLRStateList lr_)
      : base(properties_, symbols_, groups_, productions_, fa_, charsets_, lr_)
    { }
  }


  public static ParserTables? LoadParserTables(AppLog log_, string egt_file_path_name_)
  {
    try
    {
      return LoadParserTables(new SimpleDB.Reader(egt_file_path_name_));
    }
    catch (Exception e)
    {
      log_.Add(AppLogSection.System, AppLogAlert.Critical, "Exeption in LoadParserTables", e.ToString());
      return null;
    }
  }

  public static ParserTables LoadParserTables(SimpleDB.Reader egt_file_reader_)
  {
    string s_reader_header = egt_file_reader_.Header();

    if (s_reader_header == GppGlobal.s_GOLD_Parser_Tables_v1_0)
      return ParserDbLoader.ParserTableLoadVer1(egt_file_reader_);
    else if (s_reader_header == GppGlobal.s_GOLD_Parser_Tables_v5_0)
      return ParserDbLoader.ParserTablesLoadVer_5_0(egt_file_reader_);
    else
      throw new ArgumentException("Invalid egt file header: " + s_reader_header);
  }

  private static ParserTables ParserTablesLoadVer_5_0(SimpleDB.Reader rgt_reader_)
  {
    // 1 Properties
    // 2 EGTRecord.TableCounts

    ParserPropertiesLoader this_Properties = new ParserPropertiesLoader();

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
            SymbolListLoader        this_Symbol     = new SymbolListLoader(rgt_reader_.RetrieveInt16());
            CharacterSetListLoader  this_Charsets   = new CharacterSetListLoader(rgt_reader_.RetrieveInt16());
            ProductionListLoader    this_Production = new ProductionListLoader(rgt_reader_.RetrieveInt16());
            FAStateListLoader       this_DFA        = new FAStateListLoader(rgt_reader_.RetrieveInt16());
            LRStateListLoader       this_LALR       = new LRStateListLoader(rgt_reader_.RetrieveInt16());
            GroupListLoader         this_Group      = new GroupListLoader(rgt_reader_.RetrieveInt16());

            return ParserTablesLoadVer_5_0_Tail(rgt_reader_, this_Properties, this_Symbol, this_Charsets, this_Production, this_DFA, this_LALR, this_Group);
        }
        default:
          throw new Exception("File Error. A record of type '" + vb_compatable_ChrW((int)egt_record_code).ToString() + "' was read in egt-file head. this is not a valid code.");
      }
    }
    throw new Exception("File Error. Unexpected egt_reader_.EndOfFile().");
  }


  private static ParserTables ParserTablesLoadVer_5_0_Tail(SimpleDB.Reader EGT, ParserPropertiesLoader this_Properties, SymbolListLoader this_Symbol,
    CharacterSetListLoader this_Charsets, ProductionListLoader this_Production, FAStateListLoader this_DFA, LRStateListLoader this_LALR, GroupListLoader this_Group)
  {    
    while (!EGT.EndOfFile())
    {
      EGT.GetNextRecord();
      EGTRecord CharCode = (EGTRecord)EGT.RetrieveByte();
      switch (CharCode)
      {
        case EGTRecord.DFAState:
          {
            int     fa_state_index        = EGT.RetrieveInt16();
            bool    fa_state_accept       = EGT.RetrieveBoolean();
            int     fa_state_accept_index = EGT.RetrieveInt16();
            EGT.RetrieveEntry();          //.StoreEmpty();
            ParserSymbol? accept_symbol         = (fa_state_accept)? this_Symbol[fa_state_accept_index] : null;
            ((FAStateListLoader)this_DFA).AddFromLoaderAtIndex(fa_state_index, new ParserFAState(accept_symbol));
            while (!EGT.RecordComplete())
            {
              int charset_index     = EGT.RetrieveInt16();
              int target_dfa_state  = EGT.RetrieveInt16();
              EGT.RetrieveEntry();
              this_DFA[fa_state_index].EdgeList.Add(new ParserFAEdge(this_Charsets[charset_index], target_dfa_state));
            }
            break;
          }
        case EGTRecord.InitialStates:
          ((FAStateListLoader)this_DFA).SetInitialStateFromLoader(EGT.RetrieveInt16());
          ((LRStateListLoader)this_LALR).SetInitialStateFromLoader(EGT.RetrieveInt16());
          break;
        case EGTRecord.LRState:
          {
            int           lr_state_index  = EGT.RetrieveInt16();
            EGT.RetrieveEntry();          //.StoreEmpty()
            LRStateLoader lr_state        = new LRStateLoader();
            ((LRStateListLoader)this_LALR).AddFromLoader(lr_state_index, lr_state);
            while (!EGT.RecordComplete())
            {
              int symbol_index        = EGT.RetrieveInt16();
              int lr_action_act_type  = EGT.RetrieveInt16();
              int lr_action_act_value = EGT.RetrieveInt16();
              EGT.RetrieveEntry();
              lr_state.AddLRActionFromLoader(new ParserLRAction(this_Symbol[symbol_index], (LRActionType)lr_action_act_type, lr_action_act_value));
            }
          }
          break;
        case EGTRecord.Production:
          { 
            int production_table_index  = EGT.RetrieveInt16();
            int symbol_index            = EGT.RetrieveInt16();
            EGT.RetrieveEntry();
            ProductionLoader production = new ProductionLoader(this_Symbol[symbol_index], to_short(production_table_index));            
            while (!EGT.RecordComplete())
            {
              int handle_symbol_index = EGT.RetrieveInt16();
              production.AddHandleFromLoader(this_Symbol[handle_symbol_index]);
            }
            ((ProductionListLoader)this_Production).AddFromLoader(production_table_index, production);
          }
          break;
        case EGTRecord.Symbol:
          //TODO  При доработке .egt нужно учесть, чтобы первые два симбола были EOF, ERROR
          //      И как-то сделать чтобы не нужно было постоянно проверять.
          //      Скажем сделать отдельные EGTRecord.Symbol_Eof и EGTRecord.Symbol_Err
          int         symb_index  = EGT.RetrieveInt16();
          string      symb_name   = EGT.RetrieveString();
          SymbolType  symb_type   = (SymbolType)EGT.RetrieveInt16();
          if (symb_index == 0)
          {
            Debug.Assert(symb_type == SymbolType.End);
            ((SymbolListLoader)this_Symbol).AddFromLoader_End(new SymbolLoader(symb_name, symb_type, checked((short)symb_index)));
          }
          else if (symb_index == 1)
          {
            Debug.Assert(symb_type == SymbolType.Error);
            ((SymbolListLoader)this_Symbol).AddFromLoader_Err(new SymbolLoader(symb_name, symb_type, checked((short)symb_index)));
          }
          else
          {
            Debug.Assert(symb_type != SymbolType.End && symb_type != SymbolType.Error);
            ((SymbolListLoader)this_Symbol).AddFromLoader(new SymbolLoader(symb_name, symb_type, checked((short)symb_index)));
          }            
          break;
        case EGTRecord.CharRanges:
          {            
            int                 charset_table_index   = EGT.RetrieveInt16();
            EGT.RetrieveInt16();  //.StoreInt16(0);
            size_t              charset_ranges_count  = EGT.RetrieveInt16();
            EGT.RetrieveEntry();  //.StoreEmpty()
            CharacterSetLoader  charset               = new(charset_table_index, charset_ranges_count);
            size_t              ranges_counter        = 0;
            while (!EGT.RecordComplete())
              charset.AddFromLoaderRange(ref ranges_counter, EGT.RetrieveInt16(), EGT.RetrieveInt16());
            charset.AddFromLoaderRangesDone(ranges_counter);
            //
            ((CharacterSetListLoader)this_Charsets).AddFromLoader(charset);
          }
          break;
        case EGTRecord.Group:
          { 
            int               group_table_index = EGT.RetrieveInt16();
            string            group_Name        = EGT.RetrieveString();
            ParserSymbol      group_Container   = this_Symbol[EGT.RetrieveInt16()];
            ParserSymbol      group_Start       = this_Symbol[EGT.RetrieveInt16()];
            ParserSymbol      group_End         = this_Symbol[EGT.RetrieveInt16()];
            GroupAdvanceMode  group_Advance     = (GroupAdvanceMode)EGT.RetrieveInt16();
            GroupEndingMode   group_Ending      = (GroupEndingMode)EGT.RetrieveInt16();
            GroupLoader group = new GroupLoader(group_table_index, group_Name, group_Container, group_Start, group_End, group_Advance, group_Ending);
            EGT.RetrieveEntry();
            int group_nesting_index_max = EGT.RetrieveInt16();
            //TODO  вот так в оригинале лоадера почему-то итерация идет от нижней границы 1 до записанной в файле верхней границы. видимо артефакт бейсика
            int group_nesting_index_min = 1;  
            for(int group_nesting_index = group_nesting_index_min; group_nesting_index <= group_nesting_index_max; ++group_nesting_index)
              group.AddNestingFromLoader(EGT.RetrieveInt16());
            //
            ((SymbolLoader)group.Container).SetGroupFromLoader(group);
            ((SymbolLoader)group.Start).SetGroupFromLoader(group);
            ((SymbolLoader)group.End).SetGroupFromLoader(group);
            //
            ((GroupListLoader)this_Group).AddFromLoader(group);
          }
          break;
        default:
          throw new Exception("File Error. A record of type '" + vb_compatable_ChrW((int)CharCode).ToString() + "' was read. this_ is not a valid code.");
      }//switch
    }//while

    return new ParserTablesLoader(this_Properties, this_Symbol, this_Group, this_Production, this_DFA, this_Charsets, this_LALR);
  }



#if TODO_HERE
  //TODO  Пока просто отключил ..Ver1 чтобы под ногами не путалось.
  //      По хорошему лоадеров\сэйверов нужно делать отдельными классми и подключать\отключать особо
  public static ParserTables ParserTableLoadVer1(SimpleDB.Reader cgt_file_reader_)
  {
    Debug.Assert(false);
    throw new Exception("ParserTableLoadVer1 Not Implemented Yet..");
  }
  //
#else
  //TODO  HERE
#endif
}


