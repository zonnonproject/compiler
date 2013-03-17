//-----------------------------------------------------------------------------
//
//  Copyright (c) 2000-2013 ETH Zurich (http://www.ethz.ch) and others.
//  All rights reserved. This program and the accompanying materials
//  are made available under the terms of the Microsoft Public License.
//  which accompanies this distribution, and is available at
//  http://opensource.org/licenses/MS-PL
//
//  Contributors:
//    ETH Zurich, Native Systems Group - Initial contribution and API
//    http://zonnon.ethz.ch/contributors.html
//
//-----------------------------------------------------------------------------

/* #define DEBUG_VALIDATE */

using System;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Collections;

namespace Zonnon.RTL
{
    //////////////////////////////////////////////////////////////////////////////////

    public class CommonException : System.Exception
    {
        public long startLine;
        public int  startColumn;

		public CommonException ( long l, int c )
		{
            startLine = l;
            startColumn = c;
		}

        public override string Message
        {
            get { return " at line "+startLine.ToString()+", pos "+startColumn.ToString(); }
        }
    }

	public sealed class Halt : CommonException
	{
        public int returnCode;

		public Halt ( long l, int c, int r ) : base(l,c) { returnCode = r; }

        public override string Message
        {
            get { return "\n'halt' procedure returns the code " + returnCode.ToString(); }
        }

        public static void stopTheProgram(int l, int c, int r)
        {
            throw new Halt(l, c, r);
        }
	}

    public sealed class InputError : CommonException
    {
        public InputError ( long l, int c ) : base(l,c) { }

        public override string Message
        {
            get { return "\nRead exception: an error while reading"+base.Message; }
        }
    }

 // System.DivideByZeroException
 //
 // public sealed class ZeroDivide : CommonException
 // {
 //     public ZeroDivide ( long l, int c ) : base(l,c) { }
 // }

 // System.OverflowException
 //
 // public sealed class OverflowError : CommonException
 // {
 //     public OverflowError ( long l, int c ) : base(l,c) { }
 // }

    // RM System.IndexOutOfRangeException is used instead of Zonnon.RTL.RangeError
    //public sealed class RangeError : CommonException
    //{
    //    public RangeError ( long l, int c ) : base(l,c) { }

    //    public override string Message
    //    {            
    //        get {                 
    //            return "\nOutOfRange exception: an illegal range"+base.Message; }
    //    }
    //}

 // System.NullReferenceException
 //
 // public sealed class InstanceError : CommonException
 // {
 //     public InstanceError ( long l, int c ) : base(l,c) { }
 // }

    public sealed class CaseError : CommonException
    {
        public CaseError ( long l, int c ) : base(l,c) { }

        public override string Message
        {
            get { return "\nUnmatchedCase exception: ELSE branch in CASE statement missed"+base.Message; }
        }
    }

 // System.InvalidCastException
 //
 // public sealed class CastError : CommonException
 // {
 //     public CastError ( long l, int c ) : base(l,c) { }
 // }

    public sealed class ActivityError : CommonException
    {
        public ActivityError ( long l, int c ) : base(l,c) { }

        public override string Message
        {
            get { return "\nACTIVITY_ERROR exception: attempt to re-activate the activity"+base.Message; }
        }
    }

    public class ProtocolMismatch : CommonException
    {
        public ProtocolMismatch ( long l, int c ) : base(l,c) { }

        public override string Message
        {
            get { return "\nProtocolMismatch exception: activity communication algorithm mismatches the protocol"+base.Message; }
        }
    }

    public sealed class ProtocolServerMismatch : ProtocolMismatch
    {
        public ProtocolServerMismatch(long l, int c) : base(l, c) { }

        public override string Message
        {
            get { return "\nProtocolServerMismatch exception: activity communication algorithm mismatches the protocol" + base.Message; }
        }
    }

    /////////////////////////////////////////////////////////////////////////////
    public class MathException : System.Exception
    {
        public long startLine;
        public int startColumn;

        public MathException(long l, int c)
        {
            startLine = l;
            startColumn = c;
        }

        public override string Message
        {
            get { return " at line " + startLine.ToString() + ", pos " + startColumn.ToString(); }
        }
    }

    public sealed class IncompatibleSizes : MathException 
    {
        public IncompatibleSizes(long l, int c) : base(l, c) { }

        public override string Message
        {
            get { return "\nIncompatibleSizes exception: arrays have incompatible sizes" + base.Message; }
        }
    }

    public sealed class DiagonalElements : MathException
    {
        public DiagonalElements(long l, int c) : base(l, c) { }

        public override string Message
        {
            get { return "\nDiagonalElementsException: one or more diagonal element is equal to zero" + base.Message; }
        }
    }

    public sealed class NoSLUSolution : MathException
    {
        public NoSLUSolution(long l, int c) : base(l, c) { }

        public override string Message
        {
            get { return "\nNoSLUSolution exception: the solution for this system of linear equations doesn't exist" + base.Message; }
        }
    }

    public sealed class SparseTypeException : MathException
    {
        private string message;

        public SparseTypeException(long l, int c) : base(l, c) { message = ""; }
        public SparseTypeException(long l, int c, string message) : base(l, c) { message = this.message; }

        public override string Message
        {
            get
            {
                if (message != "")
                    return "\nSparse type exception : " + message + base.Message;
                else
                    return "\nSparse type exception : unknown error" + base.Message;
            }
        }
    }

    /////////////////////////////////////////////////////////////////////////////
    public sealed class Math
    {
        /// <summary>
        /// This routine searches all the elements equal to obj in array;
        /// the result is indices where obj are situated in the array
        /// </summary>
        /// <param name="obj">the object to search</param>
        /// <param name="array">array in which this function is searching</param>
        /// <returns></returns>
        public static Int32[] find(object obj, System.Array array)
        {
            int n = array.GetLength(0);
            int res_n = 0;
            int i;

            for (i = 0; i < n; i++)
            {
                if (array.GetValue(i).Equals(obj))
                    res_n++;
            }

            if (res_n == 0) return null;
            Int32[] res = new Int32[res_n];

            i = 0;
            for (int j = 0; j < res_n; j++)
            {
                while (!(array.GetValue(i).Equals(obj))) 
                    i++;
                res[j] = i++;
            }

            return res;
        }
    }

    /////////////////////////////////////////////////////////////////////////////

    public sealed class Comparator
    {
        private Comparator() { }

        // str = ((result < 0) ? "less than" : ((result > 0) ? "greater than" : "equal to"));
        // ----------------------------------------------------------------------------------
        public static bool less    ( string s1, string s2 ) { return String.Compare(s1,s2)  < 0; }
        public static bool greater ( string s1, string s2 ) { return String.Compare(s1,s2)  > 0; }
        public static bool equal   ( string s1, string s2 ) { return String.Compare(s1,s2) == 0; }

        public static bool less    ( string s1, char[] a2 ) { string s2 = new string(a2); return less(s1,s2); }
        public static bool greater ( string s1, char[] a2 ) { string s2 = new string(a2); return greater(s1,s2); }
        public static bool equal   ( string s1, char[] a2 ) { string s2 = new string(a2); return equal(s1,s2); }

        public static bool less    ( char[] a1, string s2 ) { return !less(s2,a1); }
        public static bool greater ( char[] a1, string s2 ) { return !greater(s2,a1); }
        public static bool equal   ( char[] a1, string s2 ) { return equal(s2,a1); }

        public static bool less    ( char[] a1, char[] a2 ) { string s1 = new string(a1); return less(s1,a2); }
        public static bool greater ( char[] a1, char[] a2 ) { string s1 = new string(a1); return greater(s1,a2); }
        public static bool equal   ( char[] a1, char[] a2 ) { string s1 = new string(a1); return equal(s1,a2); }
    }

    /////////////////////////////////////////////////////////////////////////////

    public sealed class Set
    {
        private Set ( ) { }  // to prevent creating instances

        public static uint constructor ( long l, int c, params object[] pars )
        {
            uint s = 0U;
            int i = 0, n = pars.Length;
            while ( i<n )
            {
                if ( pars[i] == null )
                {
                    int left = (int)pars[i+1];
                    int right = (int)pars[i+2];
                    if (left < 0 || left > 31 || right < 0 || right > 31)
                        throw new System.IndexOutOfRangeException(l.ToString() +", "+ c.ToString()+ " OutOfRange exception: an illegal range");

                    for ( int j=left; j<=right; j++ )
                        s |= 1U<<j;
                    i += 3;
                }
                else
                {
                    int p = (int)pars[i];
                    if (p < 0) throw new System.IndexOutOfRangeException(l.ToString() + ", " + c.ToString() + " OutOfRange exception: an illegal range"); ;
                    s |= (1U<<p);
                    i++;
                }
            }
            return s;
        }

//      public static bool in_set ( long l, int c, int op, uint s )
//      {
//          if ( op < 0 || op > 31 ) throw new RangeError(l,c);
//          return (s & (1U<<op)) != 0;
//      }

        public static bool in_set ( long l, int c, int op, ulong s )
        {
            if (op < 0 || op > 63) throw new System.IndexOutOfRangeException(l.ToString() + ", " + c.ToString() + " OutOfRange exception: an illegal range"); 
            return (s & (1UL<<op)) != 0;
        }

        // -s
        // Set of integers between 0 and MAX(SET)
        // which are NOT elements of s.
        //
        public static ulong complement ( ulong s ) { return ~s; }
//      public static uint  complement ( uint  s ) { return ~s; }

        // s1-s2 == s1 * (-s2)
        //
        public static ulong difference ( ulong s1, ulong s2 ) { return s1 & ~s2; }
//      public static uint  difference ( uint  s1, uint  s2 ) { return s1 & ~s2; }

        // s1 / s2 == (s1-s2)+(s2-s1)
        //
        public static ulong symm_difference ( ulong s1, ulong s2 ) { return (s1 & ~s2) | ( s2 & ~s1); }
//      public static uint  symm_difference ( uint  s1, uint  s2 ) { return (s1 & ~s2) | ( s2 & ~s1); }

        // incl(set,v)
        //
        // set = set + {v}
        //
//      public static void incl ( long l, int c, ref uint  s, int v ) 
//      { 
//          if ( v<0 || v>31 ) throw new RangeError(l,c); 
//          s |= (1U<<v); 
//      }
        public static void incl ( long l, int c, ref ulong s, int v ) 
        {
            if (v < 0 || v > 63) throw new System.IndexOutOfRangeException(l.ToString() + ", " + c.ToString() + " OutOfRange exception: an illegal range"); 
            s |= (1UL<<v); 
        }

        // excl(set,v)
        //
        // set = set - {v}
        //
//      public static void excl ( long l, int c, ref uint  s, int v )
//      {
//          if ( v<0 || v>31 ) throw new RangeError(l,c); 
//          s &= ~(1U<<v);
//      }
        public static void excl ( long l, int c, ref ulong s, int v )
        {
            if (v < 0 || v > 63) throw new System.IndexOutOfRangeException(l.ToString() + ", " + c.ToString() + " OutOfRange exception: an illegal range"); 
            s &= ~(1UL<<v);
        }
    }

    /////////////////////////////////////////////////////////////////////////////

    public struct Range
    {
        public int from, to, by;

        public Range(int from, int to, int by)
        {
            this.from = from;
            this.to = to;
            this.by = by;
        }
    }

    ///////////////////////////////////////////////////////////////////////////

    public sealed class Common
    {

        public static object _dummy = null; // For assining to whatever we don't need

        public static int throwRangeError()
        {
            throw new IndexOutOfRangeException();
        }

        public static int exponent ( int B, UInt32 N )
        {
            int res = 1;
            for ( int i=1; i<=N; i++ )
                res *= B;
            return res;
        }

        public static long exponent ( long B, UInt32 N )
        {
            long res = 1;
            for ( int i=1; i<=N; i++ )
                res *= B;
            return res;
        }

        public static double exponent ( double B, int N )
        {
            double res = 1.0;
            int NN = N>0 ? N : -N;
            for ( int i=1; i<=NN; i++ )
                res *= B;
            return N>0 ? res : 1/res;
        }
    }

    ///////////////////////////////////////////////////////////////////////////

    public sealed class Input
    {
        private Input ( ) { }  // to prevent creating instances

        private static string buf;

        public static void flush ( )
        {
            buf = ""; // System.Console.ReadLine();
        }

        public static void newline ( )
        {
            if ( buf == null || buf == "" )
                buf = System.Console.ReadLine();
        }

        public static object read ( object v )
        {
            string buf_part;

            buf = buf.Trim();
            int p = buf.IndexOf(" ");
            if ( p > -1 )
            {
                buf_part = buf.Substring(0,p);
                buf = buf.Substring(p);
            }
            else
            {
                buf_part = buf;
                buf = "";
            }

            if      ( v is bool )    v = bool.Parse(buf_part);
            else if ( v is int  )    v = int.Parse(buf_part);
            else if ( v is uint )    v = uint.Parse(buf_part);
            else if ( v is byte )    v = byte.Parse(buf_part);
            else if ( v is sbyte )   v = sbyte.Parse(buf_part);
            else if ( v is short )   v = short.Parse(buf_part);
            else if ( v is ushort )  v = ushort.Parse(buf_part);
            else if ( v is char )    v = char.Parse(buf_part);
            else if ( v is long )    v = long.Parse(buf_part);
            else if ( v is ulong )   v = ulong.Parse(buf_part);
            else if ( v is float )   v = float.Parse(buf_part);
            else if ( v is double )  v = double.Parse(buf_part);
            else if ( v is decimal ) v = decimal.Parse(buf_part);
            else if ( v is string )  v = buf_part;
            else                   { throw new InputError(0,0); }

            return v;
        }

        public static object read_string ( string s )
        {
            s = "";
            for ( int i=0, n=buf.Length; i<n; i++ )
                s += buf[i];
            return s;
        }
    }

    ///////////////////////////////////////////////////////////////////////////

    public sealed class Output
    {
        private static int leadingBlanks ( long value, int desiredWidth )
        {
            int  size = 0;
            if ( value < 0 ) { value = -value; size = 1; }
            do {
                size++; value /= 10;
            }
            while ( value != 0 );
            return System.Math.Max(0,desiredWidth-size);
        }

        private static void writeWidth ( int w )
        {
            string b = "";
            for ( int i=0; i<w; i++ ) b += " ";
            System.Console.Write(b);
        }

        public static void writeChar ( char c, int width, bool nl )
        {
            writeWidth(width-1);
            System.Console.Write(c);
            if ( nl ) System.Console.WriteLine();
        }

        public static void writeInt ( long i, int width, bool nl )
        {
            writeWidth(leadingBlanks(i,width));
            System.Console.Write(i);
            if ( nl ) System.Console.WriteLine();
        }

        public static void writeString ( string s, int width, bool nl )
        {
            writeWidth(System.Math.Max(0,width-s.Length));
            System.Console.Write(s);
            if ( nl ) System.Console.WriteLine();
        }

        public static void writeBool ( bool b, int width, bool nl )
        {
            string s = b ? "true" : "false";
            writeWidth(System.Math.Max(0,width-s.Length));
            System.Console.Write(s);
            if ( nl ) System.Console.WriteLine();
        }

        // Version for floating point format
        public static void writeReal ( double r, int width, bool nl )
        {
            string result = r.ToString("E");
            writeWidth(System.Math.Max(0,width-result.Length));
            System.Console.Write(result);
            if ( nl ) System.Console.WriteLine();
        }

        // Version for fixed-point format
        public static void writeReal ( double r, int width, int width2, bool nl )
        {
            string format = "";
            for ( int i=1; i<=width-(width2+1); i++ ) format += "#";
            format += ".";
            for ( int i=1; i<=width2; i++ ) format += "#";
            format = format + ";-" + format + ";";

            string result = r.ToString(format);
            writeWidth(System.Math.Max(0,width-result.Length));
            System.Console.Write(result);
            if ( nl ) System.Console.WriteLine();
        }
    }

    /////////////////////////////////////////////////////////////////////////
    // Concurrency and protocols runtime support
    // Version 0.0.2 of 21.12.2005
    /////////////////////////////////////////////////////////////////////////

	// Protectior
	// =======
	// Summary:
	// Monitor that protects object with 'protected' modifier.
	public class ObjectLock
	{
		ReaderWriterLock rwl = new ReaderWriterLock();

		public void EnterMethod()
		{
			rwl.AcquireWriterLock(-1);
		}

		public void LeaveMethod()
		{
            lock(rwl) Monitor.PulseAll(rwl);
			rwl.ReleaseWriterLock();            
		}

		public void EnterSharedMethod()
		{
			rwl.AcquireReaderLock(-1);
		}

		public void LeaveSharedMethod()
		{
			rwl.ReleaseReaderLock();
		}

		public ObjectLock()
		{

		}

        public void Wait()
        {
            lock (rwl)
            {
                LeaveMethod();
                Monitor.Wait(rwl);
                EnterMethod();
            }
        }

        public void WaitShared()
        {
            lock (rwl)
            {
                LeaveSharedMethod();
                Monitor.Wait(rwl);
                EnterSharedMethod();
            }
        }

	}

	// Barrier
    // =======
    // Summary:
    // Defines a lock that implements barrier in the Zonnon concurrency model.
    public class Barrier
    {
        public bool leavingNow = false;
		Int32 numAsyncOps = 0;
		AutoResetEvent opsAreDone = new AutoResetEvent(true);


        // Summary:
        // Executed by the default protocol when it starts
        public void ActivityLaunch()
        {
			if (Interlocked.Increment(ref numAsyncOps) > 0)
				opsAreDone.Reset();
        }

        // Summary:
        // Executed by the activity wrapper in the default protocol
        // when activity complete
        public void ActivityComplete()
        {
			if (Interlocked.Decrement(ref numAsyncOps) == 0)
				opsAreDone.Set();
        }

        // Summary:
        // Placed by Zonnon compiler at the end of a barrier block.
        // It is a synchronisation point.
        public void Leave()
        {
            leavingNow = true;
			opsAreDone.WaitOne(); 
        }

        // Summary:
        // Placed by Zonnon compiler at the begin of a barrier block.
        public Barrier()
        {

        }
    }

    // ZonnonAttribute
    // ========
    // Summary:
    // Class which is used for writing custom metadata.
    // Further using:
    //object[] p = typeof(NameOfClassWithCustomAttr).GetCustomAttributes(false);
    //ZonnonAttribute q = p[0] as ZonnonAttribute;        
    //Console.WriteLine(q.Data);

    public class ZonnonAttribute : Attribute
    {
        private string data;
        public string Data
        {
            get { return data; }
        }

        public ZonnonAttribute(string data_)
        { data = data_; }
    }


    // Protocol
    // ========
    // Summary:
    // Defines a default protocol for activities that do not implement any protocol
    // and it is a base class for other protocols.
    public class Protocol
    {
        public delegate void activityType ( Protocol protocol );

        protected Queue incoming = new Queue();
        protected Queue outgoing = new Queue();

        public bool externWaiting = false;
        public bool internWaiting = false;

        public int acceptExpectedOutside = 0;
        public int acceptExpectedInside = 0;

        // Summary:
        // Sends the token to activity. To be called by a client (caller).
        // 
        // Parameters:
        // obj: 
        //    Token of any type. Could be validated by a protocol. 
        // 
        // Returns:
        // Link to the protocol that allows to generate: p.send(10).send("Hi");
        //
        public Protocol send ( object obj )
        {
            lock (this)
            {
                ValidateNextIncomingToken(obj);
                incoming.Enqueue(obj);
                Monitor.Pulse(this);
            }
            return this;
        }

        // Summary:
        // Accepts the token from activity. To be called by a client (caller).
        // 
        // Returns:
        // Object that should be casted to acceptor's type.
        //
        public object receive ( )
        {
            lock (this)
            {
                ValidateNextExternalTokenRequest();
                return outgoing.Dequeue();
            }
        }

        // Summary:
        // Sends the token to caller. To be called by an activity (server).
        // 
        // Parameters:
        // obj: 
        //    Token of any type. Could be validated by a protocol. 
        // 
        // Returns:
        // Link to the protocol that allows generate: p.reply(10).reply("Hi");
        //
        public Protocol reply ( object obj )
        {
            lock (this)
            {
                ValidateNextOutgoingToken(obj);
                outgoing.Enqueue(obj);
                Monitor.Pulse(this);
            }
            return this;
        }

        // Summary:
        // Accepts the token from caller. To be called by an activity (server).
        // 
        // Returns:
        // Object that should be casted to acceptor type.
        //
        public object accept ( )
        {
            lock (this)
            {
                ValidateNextInternalTokenRequest();
                return incoming.Dequeue();
            }
        }

        // Summary:
        // Thread that runs this activity.
        Thread activityThread;

        // Summary:
        // Link to the activity body that is generated by the compiler as a procedure.
        protected activityType proc;

        // Summary:
        // Enclosing barrier that will make the caller wait for this activity.
        Barrier barrier;

        // Summary:
        // Defines if to run the activity in the pool with the limited number 
        // of threads. If the value is false then activity will be executed
        // with new separate thread. 
        // It is reccomendet to run 'quick' activities in the pool,
        // but prolonged and permanent with a separate thread
        bool runInPool; 

        // Summary:
        // Wrapper for the activity body that releases the barrier when activity ends.
        //
        public void activityBody ( )
        {
            try
            {
                proc(this); // Activity body generated by the compiler
            }
            catch ( ProtocolServerMismatch )
            {
                // We catch unhandled protocol exceptions in activities
            }
            finally
            {
                if(barrier != null) barrier.ActivityComplete();
            }
        }

		public void activityBody ( object o )
		{
			activityBody();
		}

        // Summary:
        // Real execution of the activity. Called by code generated the 
        // compiler for activity execution.
        // 
        // Parameters:
        // barrier: 
        //    a Barrier placed by compiler that defines a syncronization point.
        //
        public void invoke ( Barrier barrier )
        {            
            this.barrier = barrier;
            if(this.barrier!= null) this.barrier.ActivityLaunch();
            if ( runInPool )
            {
				ThreadPool.QueueUserWorkItem(new WaitCallback(this.activityBody));
            }
            else
            {
                activityThread = new Thread(new ThreadStart(this.activityBody));
                activityThread.Start();
            }
        }

        // Summary:
        // Enclosing barrier that will make the caller wait for this activity.
        //
        private void Init ( activityType proc, bool runInPool )
        {
            this.runInPool = runInPool;
            this.proc = proc;
            RKAP.Push((int)0);
        }
        
		public void createEBNF(int lines)
		{
			EBNF = new EBNFField[lines];
		}

		public void setEBNFLine(int line, int checkCode, bool terminal, bool receive, int next, bool endNode)
		{
			EBNF[line].checkCode = checkCode;
			EBNF[line].terminal = terminal;
			EBNF[line].receive = receive;
			EBNF[line].next = next;
			EBNF[line].endNode = endNode;
		}

		public Protocol ( activityType proc, bool runInPool )
        {
            Init(proc, runInPool);
        }

        public Protocol ( activityType proc )
        {
            Init(proc, false);
        }

        /////////////////////////////////////////////////////////////

        // EBNFField
        // ---------
        // Summary:
        // A field of a state machine 
        // 
        // Parameters:
        //
        // terminal: 
        //    if the current arc of the net is terminal 
        // receive:
        //    if the direction of current transfer is into activity then true
        // type:
        //    selects typecheck function
        // nextnode:
        //
        public struct EBNFField
        {
            public int  checkCode;
            public bool terminal;
            public bool receive;
            public int  next;
            public bool endNode;

            public EBNFField ( int checkCode, bool terminal, bool receive, int next, bool endNode )
            {
                this.checkCode = checkCode;
                this.terminal = terminal;
                this.receive = receive;
                this.next = next;
                this.endNode = endNode;
            }
        }

        public EBNFField[] EBNF = null;
        public bool protocolTerminate = false;

        // Summary:
        // This function must be overriden in the concrete protocol
        // 
        // Parameters:
        // obj: 
        //    a token to be validated 
        //
        protected virtual bool Test ( int testNumber, object obj )
        {
            //  Example of the overriding function in a concrete protocol:
            //
            //  switch (testNumber)
            //  {
            //       case 1: return obj is int;
            //       case 2: return obj is float;
            //       case 3: return obj is double;
            //       case 4: return (obj is string) && ((obj as string) == "Start session");
            //       ... for each terminal symbol from the protocol
            //  }
            //  return false;
   
            return true; // obj is object; // everything is allowed
        }

		private Stack RKAP = new Stack();
		private int currentArc = 0; //Next token

		public void Validate(bool receive, object obj)
		{
#if DEBUG_VALIDATE
            Console.WriteLine("Validating {0}", obj);
#endif
			if( EBNF == null ) return;

			// Trying to pass
			bool passed = true;
			do
			{
				passed = false;
#if DEBUG_VALIDATE
                Console.WriteLine("currentArc {0}", currentArc);
#endif
				if (EBNF[currentArc].terminal)
				{ // Terminal net
					if (EBNF[currentArc].checkCode == 0)
					{
						// Skip the arc
						currentArc = EBNF[currentArc].next;
					}
					else if (Test(EBNF[currentArc].checkCode, obj))
					{
#if DEBUG_VALIDATE
                        Console.WriteLine("test passed for {0} with {1} code", obj, EBNF[currentArc].checkCode);
#endif
						passed = true;
						// Go to next node
						if (EBNF[currentArc].next == 0)
						{ // Take from the stack
                            do
                            {
#if DEBUG_VALIDATE
                                Console.Write("return from {0} ", currentArc);
#endif
                                currentArc = (int)RKAP.Pop();
                                currentArc = EBNF[currentArc].next;
#if DEBUG_VALIDATE
                                Console.WriteLine(" to {0}", currentArc);
#endif
                            } while (currentArc == 0);
#if DEBUG_VALIDATE
                            Console.WriteLine("[next] currentArc {0}", currentArc);
#endif
						}
						else
						{
							currentArc = EBNF[currentArc].next;
#if DEBUG_VALIDATE
                            Console.WriteLine("[next] currentArc {0}", currentArc);
#endif
						}
					}
                    else if (EBNF[currentArc].endNode)
                    {
                        // Go one level up
#if DEBUG_VALIDATE
                        Console.WriteLine("test not passed for {0} with {1} code", obj, EBNF[currentArc].checkCode);
                        Console.Write("return from {0}", currentArc);
#endif
                        while (EBNF[currentArc].endNode && (RKAP.Count > 0)) currentArc = (int)RKAP.Pop();
                        if (!EBNF[currentArc].endNode)
                        {
                            
                            currentArc++;
#if DEBUG_VALIDATE
                            Console.WriteLine("to {0}", currentArc);
#endif
                        }
                        else
                        {
#if DEBUG_VALIDATE
                            Console.WriteLine("to out of the protocol -> PROTOCOL MISMATCH");
                            Console.WriteLine("test not passed for {0} with {1} code - it was last arc - not passed at all", obj, EBNF[currentArc].checkCode);
#endif
                            protocolTerminate = true;
                            Monitor.PulseAll(this);
                            if(!receive)
                                throw new ProtocolServerMismatch(0, 0);
                            else
                                throw new ProtocolMismatch(0, 0);
                        }
                    }
                    else
                    {
#if DEBUG_VALIDATE
                        Console.WriteLine("test not passed for {0} with {1} code - still have chanse", obj, EBNF[currentArc].checkCode);
#endif
                        currentArc++;
                    }
				}
				else
				{ // Subnet
#if DEBUG_VALIDATE
                    Console.WriteLine("goto from {0} to {1}", currentArc, EBNF[currentArc].checkCode);
#endif
					RKAP.Push(currentArc);
					currentArc = EBNF[currentArc].checkCode;
				}
			}while(!passed);
		}

        // Summary:
        // Checks if the token returned by caller matches the protocol syntax.
        // 
        // Parameters:
        // obj: 
        //    a token to be validated 
        //
        // Exceptions:
        //    ProtocoMistmatch in case of a mismatched token.
        //
        public void ValidateNextIncomingToken ( object obj )
        {
            if (acceptExpectedOutside > 0)
            {
                // Inside the activity we have called return. Symmetric := expected
                protocolTerminate = true;
                Monitor.PulseAll(this);
                throw new ProtocolMismatch(0, 0);
            }
            acceptExpectedInside++;
            Validate(true, obj);
        }

        // Summary:
        // Checks if this token from activity matches the protocol syntax.
        // 
        // Parameters:
        // obj: 
        //    a token to be validated 
        //
        // Exceptions:
        //    ProtocoMistmatch in case of a mismatched token.
        //
        public void ValidateNextOutgoingToken ( object obj )
        {
            if (acceptExpectedInside > 0)
            {
                // Inside the activity we have called return. Symmetric := expected
                protocolTerminate = true;
                Monitor.PulseAll(this);
                throw new ProtocolServerMismatch(0, 0);
            }
            acceptExpectedOutside++;
            Validate(false, obj);
        }

        // Summary:
        // Checks if the token request is valid
        // 
        // Exceptions:
        //    ProtocoMistmatch
        //
        public void ValidateNextExternalTokenRequest ( )
        {            
            while (outgoing.Count == 0)
            {
                if (internWaiting && (outgoing.Count == 0) && (incoming.Count == 0) || protocolTerminate)
                { // Both ends are waiting. Error
                    protocolTerminate = true;
                    Monitor.PulseAll(this);
                    throw new ProtocolMismatch(0, 0);
                }
                externWaiting = true;
                Monitor.Wait(this);
            }
            if (protocolTerminate) throw new ProtocolMismatch(0, 0);
            externWaiting = false;
            acceptExpectedOutside--;
        }

        // Summary:
        // Checks if the token request is valid
        // 
        // Exceptions:
        //    ProtocoMistmatch
        //
        public void ValidateNextInternalTokenRequest ( )
        {            
            while (incoming.Count == 0)
            {
                if (externWaiting && (incoming.Count == 0) && (outgoing.Count == 0) || protocolTerminate)
                { // Both ends are waiting. Error
                    protocolTerminate = true;
                    Monitor.PulseAll(this);
                    throw new ProtocolServerMismatch(0, 0);
                }
                internWaiting = true;
                Monitor.Wait(this);
            }
            if (protocolTerminate) throw new ProtocolServerMismatch(0, 0); 
            internWaiting = false;
            acceptExpectedInside--;
        }
    }
}
