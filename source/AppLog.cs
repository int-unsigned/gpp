//
using System.Diagnostics;
//
namespace gpp.builder;


public enum AppProgramMode
{
  Startup,
  Idle,
  Input,
  NFACase,
  NFAClosure,
  BuildingNFA,
  BuildingDFA,
  BuildingFirstSets,
  BuildingLALRClosure,
  BuildingLALR,
}


internal interface AppNotify
{
  public void Started(string text_);
  public string Text          { get; set; }
  public void Completed(string text_);
  public int Counter          { get; set; }
  public int Analyzed         { get; set; }
  public AppProgramMode Mode  { get; set; }
}
//
internal class AppNotifyDummy : AppNotify
{
  public void Started(string text_) { }
  public string Text { get; set; }
  public void Completed(string text_) { }
  public int Counter { get; set; }
  public int Analyzed { get; set; }
  public AppProgramMode Mode { get; set; }
}


internal interface AppSite
{ 
  public AppNotify  Notify   { get; }
  public AppLog     Log      { get; }
  public AppSite    CreateAppSiteForThread();
  //public void       CombineLog(AppLog other_log_);
  public void CombineLog(params AppLog[] other_logs_);
}


public enum AppLogSection
{
  Internal = -1, // 0xFFFFFFFF
  System = 0,
  Grammar = 1,
  DFA = 2,
  LALR = 3,
  CommandLine = 4,
}


public enum AppLogAlert
{
  Success = 1,
  Warning = 2,
  Critical = 3,
  Detail = 4,
  Info = 5,
}


public class AppLogItem
{
  public AppLogSection  Section;
  public AppLogAlert    Alert;
  public string         Title;
  public string         Description;
  public string         Index;
  //
  public AppLogItem(AppLogSection section_, AppLogAlert alert_, string title_, string description_, string index_)    
  { 
    Section     = section_;
    Alert       = alert_;
    Title       = title_;
    Description = description_;
    Index       = index_;
  }
  public AppLogItem(AppLogSection section_, AppLogAlert alert_, string title_, string description_)
    : this(section_, alert_, title_, description_, "")
  { }
  public AppLogItem(AppLogSection section_, AppLogAlert alert_, string title_)
    : this(section_, alert_, title_, "", "")
  { }
  //
  public string SectionName()
  {
    switch (this.Section)
    {
      case AppLogSection.Internal:      return "Internal";
      case AppLogSection.System:        return "System";
      case AppLogSection.Grammar:       return "Grammar";
      case AppLogSection.DFA:           return "DFA States";
      case AppLogSection.LALR:          return "LALR States";
      case AppLogSection.CommandLine:   return "Input";
      default:                          return "(Unspecified)";
    }
  }
  public string AlertName()
  {
    switch (this.Alert)
    {
      case AppLogAlert.Success:     return "Success";
      case AppLogAlert.Warning:     return "Warning";
      case AppLogAlert.Critical:    return "Error";
      default:                      return "Details";
    }
  }
}


public class AppLog
{
  private List<AppLogItem>  m_log_items;
  private size_t            m_critical_count = 0;
  private size_t            m_warnings_count = 0;
  //
  public AppLog()
  {
    m_log_items = new List<AppLogItem>();
  }
  //
  public int Count()  => m_log_items.Count;
  public AppLogItem this[int index_]
  {
    get => m_log_items[index_];
  }
  //
  private void _internal_add(AppLogItem item_)
  {
    m_log_items.Add(item_);
    if (item_.Alert == AppLogAlert.Critical)
      ++m_critical_count;
    else if(item_.Alert == AppLogAlert.Warning) 
      ++m_warnings_count;
  }

  public void Add(AppLogSection section_, AppLogAlert alert_, string title_)
  {
    this._internal_add(new AppLogItem(section_, alert_, title_));
  }
  //TODO  А почему string index_ ??? вызывается оно вроде всегда с конкретным числом 
  //      И вообще все это сплошное TODO если занятся локализацией
  public void Add(AppLogSection section_, AppLogAlert alert_, string title_, string description_, string index_)
  {
    //if (this.Locked)
    //  return;
    //TODO  вообще "наводить красивости" дело форматера вывода куда оно выводится. может оно и не нужно никому..
    string s_description = (!description_.Empty() && !description_.EndsWith('.'))? description_ + '.' : description_;
    this._internal_add(new AppLogItem(section_, alert_, title_, s_description, index_));
  }
  public void Add(AppLogSection section_, AppLogAlert alert_, string title_, string description_)
  {
    this.Add(section_, alert_, title_, description_, "");
  }
  public void Add(string title_, string description_)
  {
    this.Add(AppLogSection.Grammar, AppLogAlert.Warning, title_, description_);
  }
  public void Add(AppLogSection section_, AppLogAlert alert_, string title_, string description_, int line_)
  {
    this.Add(section_, alert_, title_, description_, line_.ToString());
  }

  public int AlertCount(AppLogAlert alert_)
  {
    int count = 0;
    foreach (AppLogItem item in m_log_items)
      if (item.Alert == alert_)
        ++count;
    //
    return count;
  }
  public bool LoggedCriticalError()
  {
    Debug.Assert(m_critical_count == AlertCount(AppLogAlert.Critical));
    return (m_critical_count > 0);
  }
  public bool LoggedWarning()
  {
    Debug.Assert(m_warnings_count == AlertCount(AppLogAlert.Warning));
    return (m_warnings_count > 0);
  }


  public void Append(params AppLog[] other_)
  {
    size_t c_append = 0;
    foreach (AppLog other_log in other_) 
      c_append += other_log.m_log_items.Count();

    m_log_items.EnsureCapacity(m_log_items.Count + c_append);

    foreach (AppLog other_log in other_) 
    {
      m_log_items.AddRange(other_log.m_log_items);
      m_critical_count += other_log.m_critical_count;
      m_warnings_count += other_log.m_warnings_count;
    }      
  }

  public string DumpSection(AppLogSection section_)
  {
    string s_dump = "";
    foreach (AppLogItem item in m_log_items)
    {
      if (item.Section == section_)
      {
        s_dump = s_dump + "* " + item.Title + "\r\n";
        if (!item.Description.Empty())
          s_dump = s_dump + "  " + item.Description + "\r\n";
      }
    }
    return s_dump;
  }
}
