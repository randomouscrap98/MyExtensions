using System;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Net;
using System.Linq;

namespace MyExtensions
{
   public class SimpleTCPPinger
   {
      public readonly int Port;
      public readonly IPAddress Address; // = "127.0.0.1";
      public const int ThreadSleepMilliseconds = 10;
      public const int MaxWaitSeconds = 5;

      //OUR CHILDREN WILL MAKE US PROUD
      public virtual string Identifier
      {
         get { return this.GetType().Name; }
      }

      public virtual int BufferSize
      {
         get { return 4096; }
      }

      protected virtual List<Action<byte[], NetworkStream>> MessageActions
      {
         get { return new List<Action<byte[], NetworkStream>>(); }
      }

      //Server crap
      private bool stop = false;
      private TcpListener server;
      private Thread authSpinner = null;

      private MyExtensions.Logging.Logger logger = MyExtensions.Logging.Logger.DefaultLogger;

      //Set up the server with the given logger. Otherwise, log to an internal logger
      public SimpleTCPPinger(IPAddress address, int port, MyExtensions.Logging.Logger logger = null)
      {
         this.Port = port;
         this.Address = address;

         if (logger != null)
            this.logger = logger;
      }

      public void Log(string message, MyExtensions.Logging.LogLevel level = MyExtensions.Logging.LogLevel.Normal)
      {
         logger.LogGeneral(message, level, Identifier);
      }

      public void Error(string message)
      {
         Log(message, MyExtensions.Logging.LogLevel.Error);
      }

      //This should (hopefully) start the authorization server
      public virtual bool Start()
      {
         //Oops, we already have a spinner for authorization stuff
         if(authSpinner != null)
         {
            Error(Identifier + " already running");
            return false;
         }

         try
         {
            IPAddress localAddr = Address; //IPAddress.Parse(Address);
            server = new TcpListener(localAddr, Port);
            server.Start();

            stop = false;
            ThreadStart work = RunAuthServer;
            authSpinner = new Thread(work);
            authSpinner.Start();
         }
         catch(Exception e)
         {
            Error(e.ToString());
            return false;
         }

         return true;
      }

      public virtual bool Running
      {
         get { return authSpinner != null && authSpinner.IsAlive; }
      }

      //Try to stop the auth server
      public virtual bool Stop()
      {
         //We're already stopped.
         if(authSpinner == null)
            return true;

         //Well whatever. We should signal the stop.
         stop = true;

         //Wait for a bit to see if the thread will stop itself. If not, we
         //need to force its hand.
         if(!SpinWait())
         {
            Log(Identifier + " thread was not stopped when asked.", MyExtensions.Logging.LogLevel.Warning);

            //Try to force the thread to stop
            try
            {
               authSpinner.Abort();

               //Oops, even with aborting, the thread would not yield
               if(!SpinWait())
                  Error(Identifier + " thread could not be forcibly stopped.");
            }
            catch (Exception e)
            {
               //Wow, aborting threw an exception. Yuck
               Error("Aborting " + Identifier + " thread threw exception: " + e.ToString());
            }
         }

         //Deallocate thread if it's finally dead.
         if(!authSpinner.IsAlive)
         {
            server.Stop();
            authSpinner = null;
            return true;
         }
         else
         {
            return false;
         }
      }

      //Wait for a bit (5 seconds) on the authorization thread. If it's still
      //running, return false.
      private bool SpinWait()
      {
         double waitTime = 0;

         //Do this for a bit while we wait for whatever the thread thinks it
         //needs to finish before actually finishing
         while(authSpinner.IsAlive && waitTime < MaxWaitSeconds)
         {
            Thread.Sleep(ThreadSleepMilliseconds);
            waitTime += ThreadSleepMilliseconds / 1000.0;
         }

         return !authSpinner.IsAlive;
      }

      //This should be run on a thread.
      private void RunAuthServer()
      {
         byte[] bytes = new byte[BufferSize];

         //Keep going until someone told us to stop
         while(!stop)
         {
            //If there's a pending connection, let's service it. It shouldn't
            //really take a lot of time, so no need to spawn a new thread...
            //hopefully. If it turns out to be a problem, I'll fix it.
            if(server.Pending())
            {
               TcpClient client = null;

               try
               {
                  //Get the client and get a stream for them
                  client = server.AcceptTcpClient();
                  NetworkStream stream = client.GetStream();

                  int i = 0;
                  double wait = 0.0;
                  List<byte> fullData = new List<byte>();

                  //First, wait for data to become available or a timeout,
                  //whatever comes first.
                  while(!stream.DataAvailable)
                  {
                     Thread.Sleep(ThreadSleepMilliseconds);
                     wait += ThreadSleepMilliseconds / 1000.0;

                     //Oops, we waited too long for a response
                     if(wait > MaxWaitSeconds)
                     {
                        throw new Exception("Read timeout reached (" + MaxWaitSeconds + " sec)");
                     }
                  }

                  //Keep reading until there's nothing left. This is kinda slow.
                  while(stream.DataAvailable)
                  {
                     i = stream.Read(bytes, 0, bytes.Length);
                     fullData.AddRange(bytes.Take(i));
                  }

                  byte[] data = fullData.ToArray();

                  foreach(var action in MessageActions)
                  {
                     action(data, stream);
                  }

                  client.Close();
               }
               catch (Exception e)
               {
                  Error(e.ToString());
               }
               finally
               {
                  //Now just stop. We're done after the response.
                  if(client != null)
                     client.Close();
               }
            } 

            Thread.Sleep(ThreadSleepMilliseconds);
         }
      }
   }
}

