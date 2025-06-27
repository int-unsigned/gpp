//
using System.Diagnostics;


//
namespace gpp.builder;
using static GrammarTables;


internal class BuilderProperty
{
  public readonly int             ID;
  public readonly string          Name;
  public readonly TIdentificator  Identificator;
  protected string                m_Value;
  //
  public string                   Value   => m_Value;
  //
  public BuilderProperty(int property_id_, string name_, string value_)
  {
    this.ID             = property_id_;
    this.Name           = name_;
    this.Identificator  = MakeIdentificator(name_);
    this.m_Value        = value_;
  }
  public BuilderProperty(int property_id_, GrammarProperty property_)    
  { 
    this.ID             = property_id_;
    this.Name           = property_.Name;
    this.Identificator  = property_.Identificator;
    this.m_Value        = property_.Value;
  }
}


//TODO  Здесь была такая функциональность как IgnorableMatchChars
//      то есть предполагалось, что в имени переменной могут быть некие из набора IgnorableChars
//      и их при поиске имени можно/нужно не учитывать.
//      то есть поиск имени проводить прудварительно удалив из имени эти IgnorableChars
//      Данная фуцнкциональность не использовалась и я ее отрубил
//      возможно в будущем к такой/похожей идее нужно вернуться..
//      но потом..
internal class BuilderPropertiesList
{
  protected enum BuilderPropertyId 
  {
    Name              = EGTProperty.Name,
    Version           = EGTProperty.Version,
    Author            = EGTProperty.Author,
    About             = EGTProperty.About,
    CharacterSet      = EGTProperty.CharacterSet,
    CharacterMapping  = EGTProperty.CharacterMapping,
    GeneratedBy       = EGTProperty.GeneratedBy,
    GeneratedDate     = EGTProperty.GeneratedDate,
    AutoWhitespace    = PropertyExtension.EGTProperty_MaxConst + 1,
    CaseSensitive,
    StartSymbol,
    VirtualTerminals,
  }
  private static string _get_property_name(BuilderPropertyId property_id_)
  {
    switch (property_id_) 
    { 
      case BuilderPropertyId.AutoWhitespace:    return "Auto Whitespace";
      case BuilderPropertyId.CaseSensitive:     return "Case Sensitive";
      case BuilderPropertyId.StartSymbol:       return "Start Symbol";
      case BuilderPropertyId.VirtualTerminals:  return "Virtual Terminals";
      default:                                  return ((EGTProperty)property_id_).GetPropertyName();
    }
  }
  protected class BuilderPropertyUpdatable : BuilderProperty
  {
    public BuilderPropertyUpdatable(int property_id_, string name_, string value_)
      : base(property_id_, name_, value_)
    { }
    public BuilderPropertyUpdatable(int property_id_, GrammarProperty property_)
      : base(property_id_, property_)
    { }
    public BuilderPropertyUpdatable(BuilderPropertyId property_id_, string value_)
      : this((int)property_id_, _get_property_name(property_id_), value_)
    { }
    //
    public void UpdateValue(string value_)
    {
      base.m_Value = value_;
    }
    public void Update(GrammarProperty property_)
    {
      UpdateValue(property_.Value.Trim());
    }
  }
  //
  private readonly List<BuilderProperty> m_data;
  //
  public BuilderPropertiesList()
  {
    m_data =
      [ new BuilderPropertyUpdatable(BuilderPropertyId.Name,              "(Untitled)"),
        new BuilderPropertyUpdatable(BuilderPropertyId.Version,           "(Not Specified)"),
        new BuilderPropertyUpdatable(BuilderPropertyId.Author,            "(Unknown)"),
        new BuilderPropertyUpdatable(BuilderPropertyId.About,             string.Empty),
        new BuilderPropertyUpdatable(BuilderPropertyId.CharacterSet,      "Unicode"),
        new BuilderPropertyUpdatable(BuilderPropertyId.CharacterMapping,  "Windows-1252"),        
        new BuilderPropertyUpdatable(BuilderPropertyId.GeneratedBy,       BuilderInfo.APP_NAME_VERSION_FULL),
        // обновляется в ComputeComplete после успешного построения таблиц
        new BuilderPropertyUpdatable(BuilderPropertyId.GeneratedDate,     string.Empty),
        // в .egt 5.2 не пишутся и парсер о них не знает
        new BuilderPropertyUpdatable(BuilderPropertyId.AutoWhitespace,    "True"),
        new BuilderPropertyUpdatable(BuilderPropertyId.CaseSensitive,     "False"),
        new BuilderPropertyUpdatable(BuilderPropertyId.StartSymbol,       string.Empty),
        new BuilderPropertyUpdatable(BuilderPropertyId.VirtualTerminals,  string.Empty),
      ];
  }
  private BuilderPropertyUpdatable this[BuilderPropertyId property_id_]
  {
    get => (BuilderPropertyUpdatable)m_data[(int)property_id_];
  }
  public string Name              => this[BuilderPropertyId.Name].Value;
  public string Version           => this[BuilderPropertyId.Version].Value;
  public string Author            => this[BuilderPropertyId.Author].Value;
  public string About             => this[BuilderPropertyId.About].Value;
  public BuilderProperty CharsetModeProperty        => this[BuilderPropertyId.CharacterSet];
  public BuilderProperty CharacterMappingProperty   => this[BuilderPropertyId.CharacterMapping];
  public BuilderProperty AutoWhitespaceProperty     => this[BuilderPropertyId.AutoWhitespace];
  public BuilderProperty CaseSensitiveProperty      => this[BuilderPropertyId.CaseSensitive];
  public string StartSymbol       => this[BuilderPropertyId.StartSymbol].Value;
  public string VirtualTerminals  => this[BuilderPropertyId.VirtualTerminals].Value;
  //
  public CharSetMode CharsetMode 
  {
    get 
    {
      string prop_charset_value = this.CharsetModeProperty.Value.ToUpperInvariant();
      if (prop_charset_value.Equals("UNICODE"))
        return CharSetMode.Unicode;
      if (prop_charset_value.Equals("ANSI"))
        return CharSetMode.ANSI; ;
      return CharSetMode.Invalid;
    }
  }
  public bool AutoWhitespace
  {
    get 
    {
      const string s_TRUE = "TRUE";
      return (this.AutoWhitespaceProperty.Value.ToUpperInvariant() == s_TRUE);
    }
  }
  public bool CaseSensitive
  {
    get
    {
      const string s_TRUE = "TRUE";
      return (this.CaseSensitiveProperty.Value.ToUpperInvariant() == s_TRUE);
    }
  }
  public CharMappingMode CharMappingMode
  {
    get
    {
      const string s_WINDOWS_1252 = "WINDOWS-1252";
      const string s_ANSI         = "ANSI";
      const string s_NONE         = "NONE";

      //TODO  We need typed propertis and analaize it at load. not calculate any time when needed. 
      string s_prop_character_mapping_VALUE = this.CharacterMappingProperty.Value.ToUpperInvariant();
      if (s_prop_character_mapping_VALUE.Equals(s_WINDOWS_1252) || s_prop_character_mapping_VALUE.Equals(s_ANSI))
        return CharMappingMode.Windows1252;
      return s_prop_character_mapping_VALUE.Equals(s_NONE) ?
        CharMappingMode.None : CharMappingMode.Invalid;
    }
  }


  public void AddGrammarDefinedProperty(GrammarProperty property_)
  {
    for (int index = 0; index < m_data.Count; ++index)
    {
      if (m_data[index].Identificator.IsEqual(in property_.Identificator))
      {
        ((BuilderPropertyUpdatable)m_data[index]).Update(property_);
        return;
      }
    }

    m_data.Add(new BuilderPropertyUpdatable(PropertyExtension.EGTProperty_Unknown, property_));
  }
  public void UpdateGeneratedDate(string value_)
  {
    this[BuilderPropertyId.GeneratedDate].UpdateValue(value_);
  }
  public void UpdateCharacterMapping(string value_)
  {    
    this[BuilderPropertyId.CharacterMapping].UpdateValue(value_);
  }


  public void AddFromLoader(int property_id_, string name_, string value_)
  {
    Debug.Assert(property_id_ >= 0);

    if (property_id_ <= PropertyExtension.EGTProperty_Max)
    {
      Debug.Assert(m_data[property_id_].Name == name_);
      ((BuilderPropertyUpdatable)m_data[property_id_]).UpdateValue(value_);
    }
    else
      m_data.Add( new BuilderPropertyUpdatable(property_id_, name_, value_));
  }
  public string GetPropertyValueForStore(EGTProperty property_id_)
  { 
    Debug.Assert((int) property_id_ >= 0 && (int)property_id_ <= PropertyExtension.EGTProperty_Max);
    return m_data[(int)property_id_].Value;
  }
}
