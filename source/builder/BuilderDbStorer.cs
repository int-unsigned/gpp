//
using System.Diagnostics;


//
//
namespace gpp.builder;



internal static class SimpleDbExtension
{
  internal static void NewRecord(this SimpleDB.Writer writer_, EGTRecord rec_)
  {
    writer_.NewRecord();
    writer_.StoreByte((byte)rec_);
  }
  internal static void Store(this SimpleDB.Writer writer_, EGTProperty property_id_, string property_name_, string property_value_)
  {
    writer_.NewRecord(EGTRecord.Property);
    writer_.StoreInt16((int)property_id_);    
    writer_.StoreString(property_name_);
    writer_.StoreString(property_value_);
  }
  internal static void Store(this SimpleDB.Writer writer_, BuilderPropertiesList properties_, EGTProperty property_)
  {
    writer_.Store(property_, property_.GetPropertyName(), properties_.GetPropertyValueForStore(property_));
  }
}


internal static class BuilderTablesStorer
{

  internal static bool BuilderTableSaveVer5(AppLog log_, string egt_file_path_name_, BuilderTables this_, BuilderLRStatesList this_LALR_, BuilderFACharsetsList this_CharSet_, BuilderFAStatesList this_DFA_)
  {
    try
    {
      BuilderTableSaveVer5(egt_file_path_name_, this_, this_LALR_, this_CharSet_, this_DFA_);
      return true;
    }
    catch (Exception e)
    {
      log_.Add(AppLogSection.System, AppLogAlert.Critical, "Exeption in BuilderTableSaveVer5", e.ToString());
      return false;
    }
  }
  internal static void BuilderTableSaveVer5(string egt_file_path_name_, BuilderTables this_, BuilderLRStatesList this_LALR_, BuilderFACharsetsList this_CharSet_, BuilderFAStatesList this_DFA_)
  {
    SimpleDB.Writer egt_writer  = new SimpleDB.Writer();
    //
    try
    {
      egt_writer.Open(egt_file_path_name_, GppGlobal.s_GOLD_Parser_Tables_v5_0);

      egt_writer.Store(this_.Properties, EGTProperty.Name);                 //0
      egt_writer.Store(this_.Properties, EGTProperty.Version);              //1
      egt_writer.Store(this_.Properties, EGTProperty.Author);               //2
      egt_writer.Store(this_.Properties, EGTProperty.About);                //3
      egt_writer.Store(this_.Properties, EGTProperty.CharacterSet);         //4
      egt_writer.Store(this_.Properties, EGTProperty.CharacterMapping);     //5
      egt_writer.Store(this_.Properties, EGTProperty.GeneratedBy);          //6
#if TEST_BUILD                                                              //7
      egt_writer.Store(EGTProperty.GeneratedDate, EGTProperty.GeneratedDate.GetPropertyName(), "2025-04-04 19:17");
#else
      egt_writer.Store(this_.Properties, EGTProperty.GeneratedDate);
#endif

      egt_writer.NewRecord( EGTRecord.TableCounts);
      egt_writer.StoreInt16(this_.Symbol.Count());
      egt_writer.StoreInt16(this_CharSet_.Count());
      egt_writer.StoreInt16(this_.Production.Count());
      egt_writer.StoreInt16(this_DFA_.Count);
      egt_writer.StoreInt16(this_LALR_.Count);
      egt_writer.StoreInt16(this_.Group.Count);

      egt_writer.NewRecord( EGTRecord.InitialStates);
      egt_writer.StoreInt16((int)this_DFA_.InitialState);
      egt_writer.StoreInt16((int)this_LALR_.InitialState);

      for (int charset_index = 0; charset_index < this_CharSet_.Count(); ++charset_index)
      {
        CharsRangeList number_range_list = this_CharSet_[(int)charset_index].RangeList();
        egt_writer.NewRecord( EGTRecord.CharRanges);
        egt_writer.StoreInt16((int)charset_index);
        egt_writer.StoreInt16(0);
        egt_writer.StoreInt16(number_range_list.Count);
        egt_writer.StoreEmpty();
        for (int number_range_index = 0; number_range_index < number_range_list.Count; ++number_range_index)
        {
          egt_writer.StoreInt16(number_range_list[(int)number_range_index].char_begin);
          egt_writer.StoreInt16(number_range_list[(int)number_range_index].char_final);
        }
      }

      for (int symbol_index = 0; symbol_index < this_.Symbol.Count(); ++symbol_index)
      {
        BuilderSymbol symb = this_.Symbol[(int)symbol_index];
        //TODO  А в дальнейшем лучше их вообще в .egt не сохранять, а тихо создавать вместе со списком
        Debug.Assert(symbol_index != 0 || symb.Type == SymbolType.End);   // Должен быть по индексу 0
        Debug.Assert(symbol_index != 1 || symb.Type == SymbolType.Error); // Должен быть по индексу 1
        egt_writer.NewRecord( EGTRecord.Symbol);
        egt_writer.StoreInt16(symbol_index);        
        egt_writer.StoreString(symb.Name);
        egt_writer.StoreInt16((int)symb.Type);
      }

      for (int group_index = 0; group_index < this_.Group.Count; ++group_index)
      {
        egt_writer.NewRecord( EGTRecord.Group);        
        egt_writer.StoreInt16((int)group_index);
        BuilderGroup group = this_.Group[(int)group_index];
        egt_writer.StoreString(group.Name);
        egt_writer.StoreInt16((int)group.Container.TableIndex);
        egt_writer.StoreInt16((int)group.Start.TableIndex);
        egt_writer.StoreInt16((int)group.End.TableIndex);
        egt_writer.StoreInt16((int)group.Advance);
        egt_writer.StoreInt16((int)group.Ending);
        egt_writer.StoreEmpty();
        egt_writer.StoreInt16(group.Nesting.Count);
        for(int group_nesting_index=0; group_nesting_index < group.Nesting.Count; ++group_nesting_index)
          egt_writer.StoreInt16(group.Nesting[(int)group_nesting_index]);
      }

      for (int production_index = 0; production_index < this_.Production.Count(); ++production_index)
      {
        egt_writer.NewRecord( EGTRecord.Production);
        egt_writer.StoreInt16((int)production_index);
        egt_writer.StoreInt16((int)this_.Production[(int)production_index].Head.TableIndex);
        egt_writer.StoreEmpty();
        for(int production_handle_index = 0; production_handle_index < this_.Production[(int)production_index].Handle().Count(); ++production_handle_index)
          egt_writer.StoreInt16((int)this_.Production[(int)production_index].Handle()[(int)production_handle_index].TableIndex);
      }

      for (int fa_state_index = 0; fa_state_index < this_DFA_.Count; ++fa_state_index)
      {
        egt_writer.NewRecord( EGTRecord.DFAState);
        egt_writer.StoreInt16((int)fa_state_index);
        if (this_DFA_[fa_state_index].Accept != null)
        {
          egt_writer.StoreBoolean(true);
          egt_writer.StoreInt16((int)this_DFA_[fa_state_index].Accept!.TableIndex);
        }
        else
        {
          egt_writer.StoreBoolean(false);
          egt_writer.StoreInt16(0);
        }

        egt_writer.StoreEmpty();

        for (int fa_edge_index = 0; fa_edge_index < this_DFA_[(int)fa_state_index].Edges().Count(); ++fa_edge_index)
        {
          egt_writer.StoreInt16(this_DFA_[(int)fa_state_index].Edges()[(int)fa_edge_index].Characters.TableIndex);
          egt_writer.StoreInt16(this_DFA_[(int)fa_state_index].Edges()[(int)fa_edge_index].TargetFAStateIndex);
          egt_writer.StoreEmpty();
        }
      }

      for (int lr_state_index = 0; lr_state_index < this_LALR_.Count; ++lr_state_index)
      {
        egt_writer.NewRecord( EGTRecord.LRState);
        egt_writer.StoreInt16((int)lr_state_index);
        egt_writer.StoreEmpty();
        for (int lr_state_act_index = 0; lr_state_act_index < this_LALR_[(int)lr_state_index].CountOfLRActions(); ++lr_state_act_index)
        {
          egt_writer.StoreInt16((int)this_LALR_[(int)lr_state_index][lr_state_act_index].Symbol.TableIndex);
          egt_writer.StoreInt16((int)this_LALR_[(int)lr_state_index][lr_state_act_index].Type);
          egt_writer.StoreInt16((int)this_LALR_[(int)lr_state_index][lr_state_act_index].Value());
          egt_writer.StoreEmpty();
        }
      }

      egt_writer.Close();
    }
    catch (Exception ex)
    {
      egt_writer.Close();
      throw;
    }
  }
}

