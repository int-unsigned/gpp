//
using System.Diagnostics;


//
namespace gpp.parser;


internal class ParserProperty
{
  public readonly int             ID;
  public readonly string          Name;
  protected string                m_Value;
  public string                   Value => m_Value;
  //
  public readonly TIdentificator  Identificator;
  //
  protected ParserProperty(int property_id_, string name_, string value_)
  {
    ID                  = property_id_;
    this.Name           = name_;
    this.m_Value        = value_;
    this.Identificator  = MakeIdentificator(name_);
  }
}


internal class ParserProperties
{
  protected class ParserPropertyUpdatable : ParserProperty
  {
    public ParserPropertyUpdatable(int property_id_, string name_, string value_)
      : base(property_id_, name_, value_)
    { }
    public ParserPropertyUpdatable(EGTProperty property_id_)
      : this((int)property_id_, property_id_.GetPropertyName(), string.Empty)
    { }
    //
    public void UpdateValue(string value_)
    {
      base.m_Value = value_;
    }
  }
  //
  protected readonly List<ParserPropertyUpdatable> m_data;
  //  
  public ParserProperties()
  {
    m_data = 
      [ new ParserPropertyUpdatable(EGTProperty.Name), 
        new ParserPropertyUpdatable(EGTProperty.Version), 
        new ParserPropertyUpdatable(EGTProperty.Author), 
        new ParserPropertyUpdatable(EGTProperty.About),
        new ParserPropertyUpdatable(EGTProperty.CharacterSet),
        new ParserPropertyUpdatable(EGTProperty.CharacterMapping),
        new ParserPropertyUpdatable(EGTProperty.GeneratedBy),
        new ParserPropertyUpdatable(EGTProperty.GeneratedDate),
      ];
  }
  //
  public ParserProperty this[EGTProperty property_id_]
  { 
    get => m_data[(int)property_id_];
  }
  public string Name              => this[EGTProperty.Name].Value;
  public string Version           => this[EGTProperty.Version].Value;
  public string Author            => this[EGTProperty.Author].Value;
  public string About             => this[EGTProperty.About].Value;
  public string CharacterSet      => this[EGTProperty.CharacterSet].Value;
  public string CharacterMapping  => this[EGTProperty.CharacterMapping].Value;
  public string GeneratedBy       => this[EGTProperty.GeneratedBy].Value;
  public string GeneratedDate     => this[EGTProperty.GeneratedDate].Value;
  //
  public bool GetPropertyValue(string property_name_, out string out_value_) 
  {
    TIdentificator id = MakeIdentificator(property_name_);
    for (int i = 0; i < m_data.Count; ++i)
    {
      if (m_data[i].Identificator.IsEqual(in id))
      {
        out_value_ = m_data[i].Value;
        return true;
      }
    }

    out_value_ = string.Empty;
    return false;
  }
}


//TODO  Не удается спрятать в лоадер, т.к. ParseTables нас создает..
//      додумать надо..
//internal class ParserPropertiesLoader : ParserProperties
//{
//  public ParserPropertiesLoader() : base()
//  { }
//  public void AddFromLoader(int property_id_, string name_, string value_)
//  {
//    Debug.Assert(property_id_ >= 0);

//    if (property_id_ <= PropertyExtension.EGTProperty_Max)
//    {
//      Debug.Assert(m_data[property_id_].Name == name_);
//      base.m_data[property_id_].UpdateValue(value_);
//    }
//    else
//      base.m_data.Add(new ParserProperties.ParserPropertyUpdatable(property_id_, name_, value_));
//  }
//}
