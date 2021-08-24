using System.Net.Sockets;
using System.Net;
using System;
using System.Timers;
using System.Threading;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;


namespace ratiereSimulator
{

public class General {
    private string version;
    private sbyte temperature;
    private sbyte temperaturemax;
    private ushort speed;
    private ushort speedmax;
    private sbyte frame;
    private uint idle;
    private ulong totalcycle; 
    public General(string _version,
                   sbyte _temperature,
                   sbyte _temperaturemax,
                   ushort _speed,
                   ushort _speedmax,
                   sbyte _frame,
                   uint _idle,
                   ulong _totalcycle) {
        this.version = _version;
        this.temperature = _temperature;
        this.temperaturemax = _temperaturemax;
        this.speed = _speed;
        this.speedmax = _speedmax;
        this.frame = _frame;
        this.idle = _idle;
        this.totalcycle= _totalcycle;
    }
    public string Version {
        get => version;
        set => version= Version;
    }
    public sbyte Temperature {
        get => temperature;
        set => temperature= Temperature;
    }
    public void settemperature(sbyte Temperature) {
        this.temperature = Temperature;
        if (Temperature > this.temperaturemax) {
            this.temperaturemax = Temperature; 
        }
    }
    public void setspeed(ushort Speed) {
        this.speed = Speed;
        if (Speed > this.speedmax) {
            this.speedmax = Speed; 
        }
    }

    public sbyte Temperaturemax {
        get => temperaturemax;
        set => temperaturemax= Temperaturemax;
    }
    public ushort Speed {
        get => speed;
        set => speed= Speed;
    }
    public ushort Speedmax {
        get => speedmax;
        set => speedmax= Speedmax;
    }
    public sbyte Frame {
        get => frame;
        set => frame= Frame;
    }
    public uint Idle {
        get => idle;
        set => idle= Idle;
    }
    public ulong Totalcycle {
        get => totalcycle;
        set => totalcycle = Totalcycle;
    }

} // General


public class TableV {
   
    private uint[] speedTable;  

    public TableV (int size){
        speedTable=new uint[size];
    }    

    public uint[] SpeedTable {
        get => speedTable;
        set => speedTable= SpeedTable;
    }
} // TableV

public class TableT {
   
    private uint[] tempTable;  

    public TableT (int size){
        tempTable=new uint[size];
    }    

    public uint[] TempTable {
        get => tempTable;
        set => tempTable= TempTable;
    }
} // TableT

public class TableL {
   
    private uint[] lame;  

    public TableL (int size){
        lame=new uint[size];
    }    

    public uint[] Lame {
        get => lame;
        set => lame= Lame;
    }
} // TableL


public class Report_data {
    private General general;          /* version de la structure */
    private TableV speedTable;  /* temps passé en marche par vitesse */
    private TableT tempTable;    /* temps de fonctionnement par degres */
    private ulong picks_counter;     /* nombre totale de duites */
    private TableL lameTable;    /* nombre de cycles par lames */
    
   public Report_data(){
        this.general = new General("2",0,0,0,0,28,0,0);
        this.speedTable = new TableV(50);
        this.tempTable = new TableT(50);
        this.picks_counter = 0;
        this.lameTable = new TableL(28); 
    }
    public General General {
        get => general;
        set => general=General;
    }
    public void settemperature(sbyte temperature) {
        this.general.settemperature(temperature);   
    }
     public void setspeed(ushort Speed) {
        this.general.setspeed(Speed);   
    }

    public ulong Picks_counter {
        get => picks_counter;
        set => picks_counter=Picks_counter;
    }
    public TableV SpeedTable {
        get => speedTable;
        set => speedTable=SpeedTable;
    }
    public TableT TempTable {
        get => tempTable;
        set => tempTable=TempTable;
    }    
    public TableL LameTable {
        get => lameTable;
        set => lameTable=LameTable;
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
            Report.SpeedTable.SpeedTable[indiceSpeed] += nbcoup;
            Report.TempTable.TempTable[indiceTemp] += nbcoup;
            Random rnd = new Random();
            for (short i=0;i<28;i++) {
                Report.LameTable.Lame[i] += (uint)rnd.Next((int)nbcoup);   
            }

            Report.Picks_counter += nbcoup; 
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

        var json = JsonConvert.SerializeObject(this.Report.General, Formatting.None)+ "\r\n";
        this.server.SendAll(json);

      
        //if (this.stream!=null) this.stream.Write(byteArray,0,byteArray.Length);     
        
        json = JsonConvert.SerializeObject(this.Report.SpeedTable, Formatting.None)+ "\r\n";
        this.server.SendAll(json);
    
        
        json = JsonConvert.SerializeObject(this.Report.TempTable, Formatting.None)+ "\r\n";
        this.server.SendAll(json);

        json = JsonConvert.SerializeObject(this.Report.LameTable, Formatting.None)+ "\r\n";
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
            listRatiere.Add(new Ratiere(ADDRESS,PORT+i));
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
        } 
        
    }

  }  //program

}
