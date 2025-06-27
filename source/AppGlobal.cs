/*
 * Вы можете указать дополнительные неявные директивы global using, добавив элементы Using (или элементы Import для проектов Visua_ Basic) в файл проекта, например так:
 *   <ItemGroup>
 *      <Using Include="System.IO.Pipes" />
 *   </ItemGroup>
 *   https://learn.microsoft.com/ru-ru/dotnet/core/project-sdk/overview#implicit-using-directives
 */
global using static gpp.AppGlobal;
global using static gpp.AppExtensions;
//
global using char_t         = int;
global using size_t         = int;
global using table_index_t  = int;
//
using System.Diagnostics;
using System.Reflection;
using System.Globalization;



namespace gpp
{
  internal static class AppExtensions
  {
    public static bool Empty(this string string_)
    {
      Debug.Assert(string_ != null);
      return string_.Length == 0;
    }
    public static string ToStringOrEmpty(this object item_)
    {
      Debug.Assert(item_ != null);
      string? s = item_.ToString();
      return (s == null)? string.Empty : s;
    }
    public static bool EmptyOrNull(this string? string_)
    {
      return (string_ == null)? true : string_.Empty();
    }

    //TODO  may be IEnumerable<TItem>.. We'll see
    public static string ToStringDelimited<TItem>(this List<TItem> items_, string delimiter_) where TItem : notnull
    {
      int items_count = items_.Count;
      if (items_count > 0)
      {         
        string s_text = items_[0].ToStringOrEmpty();
        for (int i = 1; i < items_count; ++i)
          s_text += (delimiter_ + items_[i].ToStringOrEmpty());
        return s_text;
      }
      return "";
    }

    public static char ToKleeneChar(this KleeneOp kleene_op_)
    {
      switch (kleene_op_)
      {
        case KleeneOp.Zero_Or_One:  return '?';
        case KleeneOp.One_Or_More:  return '+';
        case KleeneOp.Zero_Or_More: return '*';
      }
      Debug.Assert(false);
      return default;
    }

    public static string ToHexString (this int v) => v.ToString("X");
  }


  internal static class AppGlobal
  {
    public const table_index_t TABLE_INDEX_DEFAULT = -1;


    // In the original, symbols are sorted in the SymbolName comparison mode with NLS settings for .net-framework, not ICU for .net-core
    // in order to get binary matching .egt the project must be built
    // with the parameter <RuntimeHostConfigurationOption Include="System.Globalization.UseNls" Value="true" />
    // in .csproj (see project file)
    // https://learn.microsoft.com/en-us/dotnet/core/extensions/globalization-icu
    public static bool IsGlobalizationICUMode()
    {
      SortVersion sortVersion = CultureInfo.InvariantCulture.CompareInfo.Version;
      byte[]      bytes       = sortVersion.SortId.ToByteArray();
      int version = bytes[3] << 24 | bytes[2] << 16 | bytes[1] << 8 | bytes[0];
      return version != 0 && version == sortVersion.FullVersion;
    }


    public struct TIdentificator
    {
      private int     m_Hash;
      private string  m_Code;
      //
      public readonly int     Hash => m_Hash;
      public readonly string  Code => m_Code;
      //
      public static TIdentificator Create(string text_)
      {
        Debug.Assert(text_ != null);
        TIdentificator id;
        //TODO  мы для создания уникального идентификатора из строки всегда делаем .Trim()
        //      возможно это может приводить к малопонятным нюансам и .Trim() нужно применять выборочно и индивидуально
        //id.m_Code = text_.ToUpperInvariant();
        id.m_Code = text_.Trim().ToUpperInvariant();
        id.m_Hash = id.m_Code.GetHashCode();
        return id;
      }
      //
      public readonly bool IsEqual(ref readonly TIdentificator other_)
      {
        return (this.Hash == other_.Hash && this.Code.Equals(other_.Code));
      }
      public readonly bool IsEqualCode(ref readonly TIdentificator other_)
      {
        bool b = this.Code.Equals(other_.Code);
        Debug.Assert(b==false || (this.Hash == other_.Hash));
        return b;
      }
    }
    //
    public static TIdentificator MakeIdentificator(string name_)
    { 
      return TIdentificator.Create(name_);
    }


    public static bool is_valid_range(int begin_, int final_)
    {
      return (begin_ <= final_);      
    }
    public static bool is_valid_char(int ch_code_)
    {
      return (ch_code_ >= char.MinValue && ch_code_ <= char.MaxValue);
    }
    public static bool is_valid_char(uint ch_code_)
    {
      return (ch_code_ <= char.MaxValue);
    }

    public static char_t to_char(int char_value_)
    {
      Debug.Assert(is_valid_char(char_value_));
      return (char_t)char_value_;
    }
    public static char_t to_char(uint char_value_)
    {
      Debug.Assert(is_valid_char(char_value_));
      return (char_t)char_value_;
    }
    public static char_t char_next(char_t ch_code_)
    { 
      Debug.Assert(ch_code_ < char.MaxValue);
      return ch_code_ + 1;
    }
    public static char_t char_prev(char_t ch_code_)
    {
      Debug.Assert(ch_code_ > char.MinValue);
      return ch_code_ - 1;
    }


    public enum KleeneOp
    { // http://www.goldparser.org/doc/grammars/index.htm
      None,
      Zero_Or_More, //*   Kleene Closure. This symbol denotes 0 or more or the specified character(s)
      One_Or_More,  //+   One or more. This symbol denotes 1 or more of the specified character(s)
      Zero_Or_One   //?   Optional. This symbol denotes 0 or 1 of the specified character(s)
    }
    //TODO  Нужно пересмотреть где используются KleeneOpFromChar и KleeneOpFromString
    //      возможно где-то что-то можно упростить
    public static KleeneOp KleeneOpFromChar(char op_char_)
    {
      switch (op_char_)
      {
        case '*':           return KleeneOp.Zero_Or_More;
        case '+':           return KleeneOp.One_Or_More;
        case '?':           return KleeneOp.Zero_Or_One;
        case char.MinValue: return KleeneOp.None;
      }
      Debug.Assert(false);
      return default;
    }
    public static KleeneOp KleeneOpFromString(string op_string_)
    {
      if(op_string_.Length == 0)
        return KleeneOp.None;
      Debug.Assert(op_string_.Length == 1);
      return KleeneOpFromChar(op_string_[0]);
    }
    //

    
    public static string AppFileNameBase()  => Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
    //TODO  Один раз при старте вычислить надо
    public static string AppDir() 
    {
      string  app_path_base = System.AppContext.BaseDirectory;
      string? app_path      =  Path.GetDirectoryName(app_path_base);
      if (app_path.EmptyOrNull())
        return "";
      if (Debugger.IsAttached)
      { //TODO  also may examain Environment.GetEnvironmentVariable ["VSAPPIDNAME"]	"devenv.exe"
        //D:\dev\gpp\test\bin\Debug\net8.0-windows7.0\
        //D:\dev\gpp\test\bin\Release\net8.0-windows7.0\win-x64\gpp_builder.exe 
#if (DEBUG)
        string? app_path_arc = app_path;
#else
        string? app_path_arc = Path.GetDirectoryName(app_path);
        if (app_path_arc.EmptyOrNull())
          return "";
#endif
        string? app_path_cfg = Path.GetDirectoryName(app_path_arc);
        if (app_path_cfg.EmptyOrNull())
          return "";

        string? app_path_bin = Path.GetDirectoryName(app_path_cfg);
        if (app_path_bin.EmptyOrNull())
          return "";

        string? app_path_prj = Path.GetDirectoryName(app_path_bin);
        if (app_path_prj.EmptyOrNull())
          return "";

        app_path = app_path_prj;
      }
      return app_path!;
    }
    public static string AppDatDir()
    {
      string s_app_dir = AppDir();
      string? s_gpp_dir = Path.GetDirectoryName(s_app_dir);
      if(s_gpp_dir.EmptyOrNull())
        return s_app_dir;
      return Path.Join(s_gpp_dir, "dat");
    }
    public static string AppDataGrammarFilePathName(string dat_path_) => Path.Join(dat_path_, "gp.dat");
    public static string AppDataSetsFilePathName(string dat_path_)    => Path.Join(dat_path_, "sets.dat");
    public static string AppDataMappingFilePathName(string dat_path_) => Path.Join(dat_path_, "mapping.dat");
    //
    public static string AppDataGrammarFilePathName()                 => AppDataGrammarFilePathName(AppDatDir());
    public static string AppDataSetsFilePathName()                    => AppDataSetsFilePathName(AppDatDir());
    public static string AppDataMappingFilePathName()                 => AppDataMappingFilePathName(AppDatDir());



    public static string vb_compatable_Space(int n) => "".PadLeft(n);

    public static char vb_compatable_ChrW(int CharCode)
    {
      // VB original code for Strings.ChrW(CharCode);
      //
      //If CharCode < -32768 OrElse CharCode > 65535 Then
      //    Throw New ArgumentException(SR.Format(SR.Argument_RangeTwoBytes1, NameOf(CharCode)), NameOf(CharCode))
      //End If
      //Return Global.System.Convert.ToChar(CharCode And &HFFFFI)

      if (CharCode < -32768 || CharCode > 65535)
        throw new ArgumentException("CharCode < -32768 || CharCode > 65535");

      return System.Convert.ToChar(CharCode & 0xFFFF);
    }


    public static string LiteralFormat(string source_string_, bool always_delimit_)
    {
      //TODO  странное было форматирование - у кого-то было имя одна кавычка? артефакт?
      if (source_string_ == "'")
        return "''";
      else if (always_delimit_)
        return "'" + source_string_ + "'";
      else
      {
        foreach (char c in source_string_)
        { //TODO  Возможно для строки есть более эффективный механизм проверки что в строке есть <небуквы>
          if (!char.IsLetter(c))
            return "'" + source_string_ + "'";
        }
        return source_string_;
      }
    }

    public static void Swap<T>(ref T lhs, ref T rhs)
    {
      T temp;
      temp = lhs;
      lhs = rhs;
      rhs = temp;
    }


    //TODO: Many table indexes declared as short (or as int, or as ...) 
    //      need change to table_index_t (or be better to symbol_index_t, lr_index_t, etc..)
    //      see reference fron here
    public static short to_short(int value_)
    {
      return checked((short)value_);
    }
  }
}
