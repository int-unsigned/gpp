//
using System.Diagnostics;
using System.Runtime.CompilerServices;

//
namespace gpp.builder;


//TODO  Куда-то в глобальную область видимости надо. И у ComputeDFA с ComputeLALR тип установить.
using ComputeResultTypeLR   = (bool _lr_build_ok, BuilderLRStatesList _lr_states);
using ComputeResultTypeFA   = (bool _fa_build_ok, BuilderFACharsetsList _fa_charsets, BuilderFAStatesList _fa_states);
using ComputeResultType     = (bool _lr_build_ok, BuilderLRStatesList _lr_states, bool _fa_build_ok, BuilderFACharsetsList _fa_charsets, BuilderFAStatesList _fa_states);


internal class BuilderEngine
{
  public static (bool, BuilderTables, BuilderSymbol?) ConstructBuilderTables(AppSite app_, Builder builder_, GrammarTables grammar_tables_)
  {
    Debug.Assert(!app_.Log.LoggedCriticalError());

    app_.Notify.Mode = AppProgramMode.Input;
    app_.Notify.Started("Analyzing Grammar");
    //
      var builder_tables_construct_result = BuilderTablesConstructor.Construct(app_.Log, builder_, grammar_tables_);
    //
    app_.Notify.Completed("Analyzing Grammar Completed");
    app_.Notify.Mode = AppProgramMode.Idle;

    return builder_tables_construct_result;
  }



  private ComputeResultType compute_result_combine(ComputeResultTypeLR compute_lr_result_, ComputeResultTypeFA compute_fa_result_) 
  {
    return (compute_lr_result_._lr_build_ok, compute_lr_result_._lr_states, compute_fa_result_._fa_build_ok, compute_fa_result_._fa_charsets, compute_fa_result_._fa_states);
  }
  public ComputeResultType ComputeUseSync(AppSite app_, BuilderTables builder_tables_, BuilderSymbol start_symbol_)
  {
    ComputeResultTypeLR compute_lr_result = BuilderEngine.ComputeLALR(app_.Log , app_.Notify, builder_tables_, start_symbol_);
    ComputeResultTypeFA compute_fa_result = BuilderEngine.ComputeDFA(app_.Log, app_.Notify, builder_tables_);

    return compute_result_combine(compute_lr_result, compute_fa_result);
  }
  public ComputeResultType ComputeUseTask(AppSite app_, BuilderTables builder_tables_, BuilderSymbol start_symbol_)
  {
    AppSite app_fa = app_.CreateAppSiteForThread();
    AppSite app_lr = app_.CreateAppSiteForThread();
    Task<ComputeResultTypeFA> t_FA = new Task<ComputeResultTypeFA>(() => { return BuilderEngine.ComputeDFA(app_fa.Log, app_fa.Notify, builder_tables_); });    
    Task<ComputeResultTypeLR> t_LR = new Task<ComputeResultTypeLR>(() => { return BuilderEngine.ComputeLALR(app_lr.Log, app_lr.Notify, builder_tables_, start_symbol_); });

    t_FA.Start();
    t_LR.Start();

    t_FA.Wait();
    t_LR.Wait();

    app_.CombineLog(app_fa.Log, app_lr.Log);
    return compute_result_combine(t_LR.Result, t_FA.Result);
  }


  public static ComputeResultTypeFA ComputeDFA(AppLog fa_log_, AppNotify fa_notify_, BuilderTables builder_tables_)
  {
    fa_notify_.Started("Computing DFA States");

    bool              b_case_sensitive    = builder_tables_.Properties.CaseSensitive;
    CharMappingMode   char_mapping_mode   = builder_tables_.Properties.CharMappingMode; 

    var fa_building_result = BuilderFA.DoBuild(fa_notify_, fa_log_,
                                      builder_tables_.PredefinedUnicodeTable, 
                                      builder_tables_.Symbol, b_case_sensitive, char_mapping_mode);

    fa_notify_.Completed("DFA States Completed");

    return fa_building_result;
  }

  public static ComputeResultTypeLR ComputeLALR(AppLog lr_log_, AppNotify lr_notify_, BuilderTables builder_tables_, BuilderSymbol start_symbol_)
  {    
    lr_notify_.Started("Computing LALR Tables");    

    //TODO  Не совсем корректно выполнять это в отдельном потоке, т.к. построитель LALR первым делом в SetupNullableTable и SetupFirstTable
    //      модифицирует таблицу Симболов устанавливая поля .Nullable и .First. Эти поля как-бы внутренние для построителя (и UI)
    //      и нигде более не используются, но все же это нехорошо.
    //      Лучше было бы эти поля из общего класса Симбол вынести в иной вспомогательный класс SymbolLRExtension (таблицу) или что-то подобное.
    var build_lr_result = BuilderLR.DoBuild(lr_notify_, lr_log_, builder_tables_.Symbol, start_symbol_, builder_tables_.Production);

    lr_notify_.Completed("LALR Tables Completed");

    return build_lr_result;
  }


  public static void ComputeComplete(BuilderTables builder_tables_, DateTime generated_date_)
  {
      builder_tables_.Properties.UpdateGeneratedDate(generated_date_.ToString("yyyy-MM-dd HH:mm"));

    //TODO  unplug for while
    //BuilderApp.PopulateSaveCGTWarningTable();
  }
  public static void ComputeComplete(BuilderTables builder_tables_)
  {
    ComputeComplete(builder_tables_, DateTime.Now);

    //TODO  unplug for while
    //BuilderApp.PopulateSaveCGTWarningTable();
  }
}
