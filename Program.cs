using System.Net.Sockets;
using System.Net;
using System;
using System.Timers;
using System.Threading;
//using System.Text;
//using System.Text.Json;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;


namespace ratiereSimulator
{

public class General {
    public string Version { get ; set; }
    public sbyte Temperature { get; set; }
    public sbyte Temperaturemax { get; set; }
    public ushort Speed { get; set; }
    public ushort Speedmax { get; set; }
    public sbyte Frame { get; set; }
    public uint Idle { get; set; }
    public ulong Totalcycle { get; set; }

    public General(string version,
                   sbyte temperature,
                   sbyte temperaturemax,
                   ushort speed,
                   ushort speedmax,
                   sbyte frame,
                   uint idle,
                   ulong totalcycle) {
        this.Version = version;
        this.Temperature = temperature;
        this.Temperaturemax = temperaturemax;
        this.Speed = speed;
        this.Speedmax = speedmax;
        this.Frame = frame;
        this.Idle = idle;
        this.Totalcycle= totalcycle;
    }
    

   
    public void settemperature(sbyte Temperature) {
        this.Temperature = Temperature;
        if (Temperature > this.Temperaturemax) {
            this.Temperaturemax = Temperature; 
        }
    }
    public void setspeed(ushort Speed) {
        this.Speed = Speed;
        if (Speed > this.Speedmax) {
            this.Speedmax = Speed; 
        }
    }


} // General


public class TableV {
    public uint[] SpeedTable { get ; set ; }
    public TableV (int size){
        SpeedTable=new uint[size];
    }    

//    [JsonConstructor]
    public TableV (uint[] speedTable){
        SpeedTable=speedTable;
    } 

} // TableV

public class TableT {
   
    public uint[] TempTable { get; set; }
    public TableT (int size){
        TempTable=new uint[size];
    }    

} // TableT

public class TableL {
    public uint[] Lame { get; set;}
    public TableL (int size){
        Lame=new uint[size];
    }    
       

} // TableL


public class Report_data {

    public General General { get; set;}

    public TableV TableV { /* temps passé en marche par vitesse */
        get;
        set;
    }

    public TableT TableT {  /* temps de fonctionnement par degres */
        get;
        set;
    }
   public ulong Picks_counter { get; set;}
   public TableL TableL {  /* temps de fonctionnement par degres */
        get;
        set;
    }
    
   public Report_data(){
      
        this.General = new General("2",0,0,0,0,28,0,0);
        this.TableV = new TableV(50);
        this.TableT = new TableT(50);
        this.Picks_counter = 0;
        this.TableL = new TableL(28); 
    }

     
    public void settemperature(sbyte temperature) {
        this.General.settemperature(temperature);   
    }

     public void setspeed(ushort Speed) {
        this.General.setspeed(Speed);   
    }
   
    public Boolean Save(string fileName) {

       // var options = new JsonSerializerOptions { WriteIndented = false };
       //JsonSerializer.Serialize(this, options);
        string json = JsonConvert.SerializeObject(this);
        Console.WriteLine("saving report to "+ fileName);
        Console.WriteLine(json);
        File.WriteAllText(@fileName,json);
        return true;    
    }
    public Boolean Restore(string fileName) {
        if (File.Exists(fileName)) {
            Console.WriteLine("Restore report from "+ fileName);
            string readText = File.ReadAllText(@fileName);      
            Console.WriteLine(readText);
            
            //readText ="{\"Version\":\"2\",\"Temperature\":61,\"Temperaturemax\":61,\"Speed\":745,\"Speedmax\":745,\"Frame\":28,\"Idle\":0,\"Totalcycle\":0}";
              
            //var read_report = JsonSerializer.Deserialize<Report_data>(readText);
            Report_data read_report = JsonConvert.DeserializeObject<Report_data>(readText);
            Console.WriteLine("Deserialized done");          
     
            this.General = read_report.General;
            this.TableV = read_report.TableV;
            this.TableT = read_report.TableT;
            this.Picks_counter = read_report.Picks_counter;
            this.TableL = read_report.TableL;
        }
        
        return true;    
    }






} //Report_Data
    
public class Work {
    private int duration; /* work duration */
    private uint periode; /* work duration */
    private ushort speed; /* work speed */
    private sbyte temp; /* work temperature */
    private DateTime start;
    private Boolean running; 
  

    public Work(){
        Random rnd = new Random();
        this.duration = (int)rnd.Next(600);;
        this.periode = 5;
        this.speed = (ushort)rnd.Next(1300);
        this.temp = (sbyte)rnd.Next(99);
        this.start = DateTime.Now;
        this.running = true;
    }

    public sbyte Temp {
        get => temp;
        set => temp=Temp;
    }
    public ushort Speed {
        get => speed;
        set => speed=Speed;
    }

    public Boolean Interval(Report_data Report) {

  
        short indiceSpeed=(short)Math.Round(this.speed/60.0);
        short indiceTemp=(short)Math.Round(this.temp/2.0);
    
        uint nbcoup = (uint)Math.Round(this.periode*this.speed/60.0);
        


     
        if (this.running) {
            Report.TableV.SpeedTable[indiceSpeed] += nbcoup;
            Report.TableT.TempTable[indiceTemp] += nbcoup;
            Random rnd = new Random();
            for (short i=0;i<28;i++) {
                Report.TableL.Lame[i] += (uint)rnd.Next((int)nbcoup);   
            }

            Report.Picks_counter += nbcoup; 
            Report.General.Totalcycle = Report.Picks_counter;
        }    
        else {
            Report.General.Idle += this.periode;
        }   

        this.duration -=(int)this.periode;   
        Console.WriteLine("work remaining :" + duration +"{0}", Environment.NewLine);

        if (this.duration<=0) {
            this.running = false;  
        }
        
        return this.running; 
    }

} // Work    

public class Ratiere {
    private Report_data report;
    private Work work;
    private int port;
    private AsynchronousSocketListener server;

    public Report_data Report {
        get => report;
        set => report=Report;
    }    
    public Work Work {
        get => work;
        set => work= Work;
    }    

    public int Port {
        get => port;
        set => port=Port;
    }



    public Ratiere(String address,int portNumber) {
        report = new Report_data();
        work = new Work();
        port = portNumber;

        report.settemperature(work.Temp);
        report.setspeed(work.Speed);

        server = new AsynchronousSocketListener(); 
        
        Thread newThread;
        ThreadStart threadDelegate = new ThreadStart(this.startListeningRatiere);
        newThread = new Thread(threadDelegate);
        newThread.Start();
    }

    private void startListeningRatiere(){
        this.server.StartListening(this.port);
    }

       
    public void Interval(){

        Boolean ongoing;

        ongoing = work.Interval(report);

        string json = JsonConvert.SerializeObject(this.Report.General)+ "\r\n";
        // var json = JsonSerializer.Serialize(this.Report.General)+ "\r\n";
 
        this.server.SendAll(json);

      
        //if (this.stream!=null) this.stream.Write(byteArray,0,byteArray.Length);     
        
        json = JsonConvert.SerializeObject(this.Report.TableV)+ "\r\n";
        //  json = JsonSerializer.Serialize(this.Report.TableV)+ "\r\n";
        this.server.SendAll(json);
    
        
        json = JsonConvert.SerializeObject(this.Report.TableT)+ "\r\n";
        // json = JsonSerializer.Serialize(this.Report.TableT)+ "\r\n";
        this.server.SendAll(json);

        json = JsonConvert.SerializeObject(this.Report.TableL)+ "\r\n";
        // json = JsonSerializer.Serialize(this.Report.TableL)+ "\r\n";
        this.server.SendAll(json);
    
        if (!ongoing) {
            Console.WriteLine("work is over on this ratiere, starting a new one");
            this.work = new Work();
            Report.General.Temperature = Work.Temp;
            if (Work.Temp > Report.General.Temperaturemax) Report.General.Temperaturemax = Work.Temp;
            Report.General.Speed = (ushort)Work.Speed;
            if (Work.Speed > Report.General.Speedmax) Report.General.Speedmax = (ushort)Work.Speed;
        }
    }


} //ratiere 

class Program {
    const string ADDRESS = "127.0.0.1";
    const int PORT = 8880;   
    const int NBRATIERE = 1;   
    const int INTERVAL = 5000;
     

    
    private static System.Timers.Timer aTimer;


    

    static void Main(string[] args) {
   

        List<Ratiere> listRatiere = new List<Ratiere>();
 
        Console.WriteLine("Hello World!");

        for (int i=0;i<NBRATIERE;i++) {
            Console.WriteLine("Create new ratiere {0}:", i);
            Ratiere ratiere = new Ratiere(ADDRESS,PORT+i);
            ratiere.Report.Restore(".\\report_" + ratiere.Port.ToString());
            listRatiere.Add(ratiere);
        }

        aTimer = new System.Timers.Timer();
        aTimer.Interval = INTERVAL;

        // Hook up the Elapsed event for the timer. 
        aTimer.Elapsed += (sender,e) => OnTimedEvent(sender,e,listRatiere);   
        
        aTimer.AutoReset = true;

        // Start the timer
        aTimer.Enabled = true;
        
        //enter to an infinite cycle to be able to handle every change in stream
        while(true){
          
        };

    }



     
    private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e, List<Ratiere> listratiere) {
       
        Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
        
        foreach (Ratiere ratiere in listratiere) {
            ratiere.Interval(); 
            ratiere.Report.Save(".\\report_" + ratiere.Port.ToString());
        } 
        
    }

  }  //program

}
