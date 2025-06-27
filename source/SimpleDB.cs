//
using System.Diagnostics;

//TODO  Ну тут еще много хорошего сделать можно.. Переписать все нахрен..
#nullable disable


internal enum EGTRecord : byte
{
  CharSetLiteral = 67,  // 0x43
  DFAState = 68,        // 0x44
  InitialStates = 73,   // 0x49
  LRState = 76,         // 0x4C
  ParamRecord = 80,     // 0x50
  Production = 82,      // 0x52
  Symbol = 83,          // 0x53
  Counts_1 = 84,        // 0x54
  CharRanges = 99,      // 0x63
  Group = 103,          // 0x67
  Property = 112,       // 0x70
  TableCounts = 116,    // 0x74
}




internal sealed class SimpleDB
{
  private const short kRecordContentMulti = 77; //TODO  Используется только при записи. При чтении видимо где-то тихо игнорируется

  
  public enum EntryType : byte
  {
    Error     = 0,
    Boolean   = 66, // 0x42
    Empty     = 69, // 0x45
    UInt16    = 73, // 0x49
    String    = 83, // 0x53
    Byte      = 98, // 0x62
  }


  public class IOException : Exception
  {
    public IOException(string Message) : base(Message)
    { }
    public IOException(string Message, Exception Inner) : base(Message, Inner)
    { }
    public IOException(SimpleDB.EntryType Type, BinaryReader Reader)
      : base( "Type mismatch in file. Read '" + vb_compatable_ChrW((int) Type).ToString() + "' at " + Reader.BaseStream.Position.ToString() )
    { }
  }


  public class Entry
  {
    public SimpleDB.EntryType Type;
    public object             Value;
    //
    public Entry()
    {
      this.Type = SimpleDB.EntryType.Empty;
      this.Value = (object) "";
    }
    public Entry(SimpleDB.EntryType Type, object Value)
    {
      this.Type = Type;
      this.Value = Value;
    }

    public bool ToBoolean() 
    { 
      return (bool) this.Value;
    }
    public byte ToByte()
    {
      return (byte)this.Value;
    }
    public ushort ToUInt16()
    {
      return (ushort)this.Value;
    }
    public int ToInt32()
    {
      return (int)this.Value;
    }
  }


  public class Reader
  {
    private const byte kRecordContentMulti = 77;
    //
    private string        m_FileHeader;
    private BinaryReader  m_Reader;
    private int           m_EntryCount;
    private int           m_EntriesRead;

    
    public Reader()
    {  }
    public Reader(string egt_file_path_name_)
    {
      this.Open(egt_file_path_name_);
    }


    public bool RecordComplete() => this.m_EntriesRead >= this.m_EntryCount;

    public void Close()
    {
      if (this.m_Reader == null)
        return;
      this.m_Reader.Close();
      this.m_Reader = (BinaryReader) null;
    }

    public short EntryCount()   => checked ((short) this.m_EntryCount);
    public short EntriesRead()  => checked((short)this.m_EntriesRead);
    public bool EndOfFile()     => this.m_Reader.BaseStream.Position == this.m_Reader.BaseStream.Length;
    public string Header()      => this.m_FileHeader;

    public void Open(BinaryReader reader_)
    {
      this.m_Reader       = reader_;
      this.m_EntryCount   = 0;
      this.m_EntriesRead  = 0;
      this.m_FileHeader   = this.RawReadCString();
    }
    public void Open(string file_path_name_extension_)
    {
      this.Open(new BinaryReader((Stream) File.Open(file_path_name_extension_, FileMode.Open, FileAccess.Read, FileShare.Read)));
    }

    public SimpleDB.Entry RetrieveEntry()
    {
      SimpleDB.Entry entry = new SimpleDB.Entry();
      if (this.RecordComplete())
        throw new SimpleDB.IOException("Read past end of record at " + this.m_Reader.BaseStream.Position.ToString());
      
      ++this.m_EntriesRead;

      byte type_byte = this.m_Reader.ReadByte();
      entry.Type = (SimpleDB.EntryType) type_byte;
      switch (type_byte)
      {
        case 66:  //Boolean
          byte bool_byte = this.m_Reader.ReadByte();
          entry.Value = (bool)(bool_byte == (byte)1);
          break;
        case 69:  //Empty
          entry.Value = "";
          break;
        case 73:  //UInt16
          entry.Value =  this.RawReadUInt16();
          break;
        case 83:  //String
          entry.Value = this.RawReadCString();
          break;
        case 98:  //Byte
          entry.Value = this.m_Reader.ReadByte();
          break;
        default:
          entry.Type = SimpleDB.EntryType.Error;
          entry.Value = "";
          break;
      }
      return entry;
    }

    private ushort RawReadUInt16()
    {
      int num = (int) this.m_Reader.ReadByte();
      return checked ((ushort) (((int) this.m_Reader.ReadByte() << 8) + num));
    }

    private string RawReadCString()
    {
      string str = "";
      bool flag = false;
      while (!flag)
      {
        ushort CharCode = this.RawReadUInt16();
        if (CharCode == (ushort) 0)
          flag = true;
        else
          str += vb_compatable_ChrW((int)CharCode).ToString();
      }
      return str;
    }

    public string RetrieveString()
    {
      SimpleDB.Entry entry = this.RetrieveEntry();      
      return entry.Type == SimpleDB.EntryType.String ? entry.Value.ToString() : throw new SimpleDB.IOException(entry.Type, this.m_Reader);
    }
    public int RetrieveInt16()
    {
      SimpleDB.Entry entry = this.RetrieveEntry();
      return entry.Type == SimpleDB.EntryType.UInt16 ? entry.ToUInt16() : throw new SimpleDB.IOException(entry.Type, this.m_Reader);
    }
    public bool RetrieveBoolean()
    {
      SimpleDB.Entry entry = this.RetrieveEntry();
      return entry.Type == SimpleDB.EntryType.Boolean ? entry.ToBoolean() : throw new SimpleDB.IOException(entry.Type, this.m_Reader);
    }
    public byte RetrieveByte()
    {
      SimpleDB.Entry entry = this.RetrieveEntry();
      return entry.Type == SimpleDB.EntryType.Byte ? entry.ToByte() : throw new SimpleDB.IOException(entry.Type, this.m_Reader);
    }

    public bool GetNextRecord()
    {
      while (this.m_EntriesRead < this.m_EntryCount)
        this.RetrieveEntry();
      bool nextRecord;
      if (this.m_Reader.ReadByte() == (byte) 77)
      {
        this.m_EntryCount = (int) this.RawReadUInt16();
        this.m_EntriesRead = 0;
        nextRecord = true;
      }
      else
        nextRecord = false;
      return nextRecord;
    }
  } // class Reader

  
  public class EntryList
  {
    private List<Entry> m_data;
    //
    public EntryList() => m_data = new List<Entry>();
    //
    public Entry this[int index_]  => m_data[index_];
    public void Add(Entry value_)  => m_data.Add(value_);
    public void Clear()            => m_data.Clear();
    public int Count               => m_data.Count;
  }


  
  public class Writer
  {
    private FileStream          m_File;
    private BinaryWriter        m_Writer;
    private SimpleDB.EntryList  m_CurrentRecord;
    private string              m_ErrorDescription;

    public Writer() => this.m_CurrentRecord = new SimpleDB.EntryList();

    public string ErrorDescription() => this.m_ErrorDescription;

    public void Close()
    {
      this.WriteRecord();
      this.m_File.Close();
    }

    public void Open(string Path, string Header)
    {
      try
      {
        this.m_File = new FileStream(Path, FileMode.Create);
        this.m_Writer = new BinaryWriter((Stream) this.m_File);
        this.RawWriteCString(Header);
      }
      catch (Exception ex)
      {
        throw new SimpleDB.IOException("Could not open file", ex);
      }
    }

    public void StoreEmpty()
    {
      this.m_CurrentRecord.Add(new SimpleDB.Entry(SimpleDB.EntryType.Empty, (object) ""));
    }

    public void StoreBoolean(bool Value)
    {
      this.m_CurrentRecord.Add(new SimpleDB.Entry(SimpleDB.EntryType.Boolean, (object) Value));
    }

    public void StoreInt16(int Value)
    {
      this.m_CurrentRecord.Add(new SimpleDB.Entry(SimpleDB.EntryType.UInt16, (object) Value));
    }

    public void StoreByte(byte Value)
    {
      this.m_CurrentRecord.Add(new SimpleDB.Entry(SimpleDB.EntryType.Byte, (object) Value));
    }

    public void StoreString(string Value)
    {
      this.m_CurrentRecord.Add(new SimpleDB.Entry(SimpleDB.EntryType.String, (object) Value));
    }

    private void RawWriteCString(string Text)
    {
      int num = checked (Text.Length - 1);
      int index = 0;
      while (index <= num)
      {
        this.RawWriteInt16((int) Text[index]);
        checked { ++index; }
      }
      this.RawWriteInt16(0);
    }

    private void RawWriteInt16(int Value)
    {
      byte num1 = checked ((byte) (Value & (int) byte.MaxValue));
      byte num2 = checked ((byte) (Value >> 8 & (int) byte.MaxValue));
      this.m_Writer.Write(num1);
      this.m_Writer.Write(num2);
    }

    private void RawWriteInt32(int Value)
    {
      byte num1 = checked ((byte) (Value & (int) byte.MaxValue));
      byte num2 = checked ((byte) (Value >> 8 & (int) byte.MaxValue));
      num2 = checked ((byte) (Value >> 16 & (int) byte.MaxValue));
      byte num3 = checked ((byte) (Value >> 24 & (int) byte.MaxValue));
      this.m_Writer.Write(num1);
      this.m_Writer.Write(num3);
      byte num4 = 0;
      this.m_Writer.Write(num4);
      byte num5 = 0;
      this.m_Writer.Write(num5);
    }

    private void RawWriteByte(byte Value) => this.m_Writer.Write(Value);

    public void NewRecord() => this.WriteRecord();

    private void WriteRecord()
    {
      int record_count = this.m_CurrentRecord.Count;
      Debug.Assert(record_count >= 0);
      if (record_count == 0)
        return;

      this.RawWriteByte((byte)kRecordContentMulti /*77*/);
      this.RawWriteInt16(record_count);

      for (int i = 0; i < record_count; ++i)
      {
        SimpleDB.Entry entry = this.m_CurrentRecord[i];
        switch (entry.Type)
        {
          case SimpleDB.EntryType.Boolean:
            this.RawWriteByte((byte)EntryType.Boolean);
            this.RawWriteByte((byte)(entry.ToBoolean()? 1 : 0));
            break;
          case SimpleDB.EntryType.UInt16:
            this.RawWriteByte((byte)EntryType.UInt16);
            this.RawWriteInt16(entry.ToInt32());    //TODO  А вот здесь интересно - запись типа .UInt16 пишется как .ToInt32
            break;
          case SimpleDB.EntryType.String:
            this.RawWriteByte((byte)EntryType.String);
            SimpleDB.Entry entry2 = entry;
            string Text = entry2.Value.ToString();
            this.RawWriteCString(Text);
            entry2.Value = (object)Text;
            break;
          case SimpleDB.EntryType.Byte:
            this.RawWriteByte((byte)EntryType.Byte);
            this.RawWriteByte(entry.ToByte());
            break;
          default:
            this.RawWriteByte((byte)EntryType.Empty /*69*/);  //TODO  Хм.. если ничего не понятно - пишется .Empty, .Error вроде есть ?
            break;
        }
      }

      this.m_CurrentRecord.Clear();
    }

  } //class Writer

} // class SimpleDB
