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

using System;
using System.Collections.Generic;
using System.Compiler;
using System.Xml;
using System.Diagnostics;

// TO DO: To write normal comments as in RTL.cs

namespace ETH.Zonnon
{
    // protocol P = (* Syntax of the protocol *) 
    //     (
    //         (* Keywords *)
    //         RUNAWAY, CHASE, KO, ATTACK, DEFENSE, LEG, NECK, HEAD,
    //
    //         (* Productions *)
    //         attack = ATTACK strike,
    //         defense = DEFENSE strike,
    //         strike = bodypart [ strength ],
    //         bodypart = LEG | NECK | HEAD,
    //         strength = integer,
    //         fight = { attack ( { defense  attack } | RUNAWAY [ ?CHASE] | KO | fight ) }
    //     );

 
    
    // Summary:
    // This class generates function Test for the protocol
    // 
    //  protected override bool Test(int testNumber, object obj)
    //  {
    //      switch (testNumber)
    //      {
    //          case 1: return (obj is KeyWords) && ((KeyWords)obj == KeyWords.START_TEXT);
    //          case 2: return obj is string;
    //          case 3: return (obj is KeyWords) && ((KeyWords)obj == KeyWords.MODIFIER1);
    //          case 4: return (obj is KeyWords) && ((KeyWords)obj == KeyWords.MODIFIER2);
    //          case 5: return (obj is KeyWords) && ((KeyWords)obj == KeyWords.END_TEXT);
    //          case 6: return obj is int;
    //          case 7: return (obj is int) && ((int)obj == 7883);
    //          case 8: return (obj is int) && ((int)obj == 1221);
    //          case 9: return (obj is int) && ((int)obj == 1133);
    //      }
    //      return false;
    //  }
    public class TestFunctionGenerator
    {
		public enum TestType {ConstantTest, TypeTest, TerminalTest};
		public class Test
		{
			public TestType type;
			public LITERAL literal = null; // For constant test
			public TYPE type_name = null;  // For type test
			public ENUMERATOR_DECL enumerator = null; // For terminal test
			public Test(LITERAL literal)
			{
				type = TestType.ConstantTest;
				this.literal = literal;
			}
			public Test(TYPE type_name)
			{
				type = TestType.TypeTest;
				this.type_name = type_name;
			}
			public Test(ENUMERATOR_DECL enumerator)
			{
				type = TestType.TerminalTest;
				this.enumerator = enumerator;
			}

            public override string ToString()
            {
                switch (type)
                {
                    case TestType.ConstantTest:
                        return type.ToString() +"("+ literal.Name +")";                        
                    case TestType.TypeTest:                        
                        return type_name.Name;                        
                    case TestType.TerminalTest:
                    default:
                        return enumerator.Name;
                }
            }

#if DEBUG
			public void report()
			{
				switch(type)
				{
					case TestType.ConstantTest:	
                        System.Console.WriteLine("{0} \t\t {1} ", type, literal.name);
						break;
					case TestType.TypeTest:		
                        System.Console.WriteLine("{0} \t\t {1} ", type, type_name.name);
						break;
					case TestType.TerminalTest: 
                        System.Console.WriteLine("{0} \t\t {1} ", type, enumerator.name);
						break;
				}
			}
#endif
		}
        public List<Test> tests = new List<Test>();

		// Summary:
        // 
        // Return value:
        //   code for this test. Probably not new if there was a similar registration
        //   before the code can be reused.
        // Example:
        //        case 7: return (obj is int) && ((int)obj == 7883);
        //        literal represents 7883 and includes information about the type
        //        return value is 7. This value will be used in the corresponding table.
        public int ReqisterConstantTest(LITERAL literal)
        {
			tests.Add(new Test(literal));
			return tests.Count;
        }
        public int RegisterTypeTest(TYPE type_name)
        {
			tests.Add(new Test(type_name));
            return tests.Count;
        }
        public int RegisterTerminalTest(ENUMERATOR_DECL enumerator)
        {
			tests.Add(new Test(enumerator));
            return tests.Count;
        }

#if DEBUG
		public void report()
		{
			System.Console.WriteLine("#\ttype of test \t\t details");
			for(int i=1; i<tests.Count+1; i++)
			{
				System.Console.Write("{0}:\t", i);
				(tests[i-1] as Test).report();
			}
		}
#endif
    }
	
    public class TableGenerator
    {

        // All terminal symbols recursively substituted
        public struct SEBNF
        {
            public int checkCode;
            public bool receive;
            public int next;
            public bool endNode;

            public override string ToString()
            {
                return string.Format("{0} \t\t {1} \t\t {2} \t\t {3}", checkCode, receive?"receive":"send   ", next, endNode?"end":"or ");
            }
        }

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
#if DEBUG
			public void report()
			{
				System.Console.WriteLine("{0} \t\t {1} \t\t {2} \t\t {3} \t\t {4}", checkCode, terminal, receive, next, endNode );
			}
#endif
            public override string ToString()
            {
                return string.Format("{0} \t\t {1} \t\t {2} \t\t {3} \t\t {4}", checkCode, terminal, receive, next, endNode);
            }
		}

		bool valid = true;

		// Nets for temproral representation
		System.Collections.ArrayList nets = new System.Collections.ArrayList();

		System.Collections.ArrayList nodelist = new System.Collections.ArrayList();

		int arcnumber = 0;
		
		// Final table
		public EBNFField[] EBNF = null;

        public class Node
        {
			TableGenerator tg;
            public bool returnNode = false;
            private System.Collections.ArrayList arcs = new System.Collections.ArrayList();
			private NodeElement ZeroArc = null;
			private int count = 0;

            public void AddArc(NodeElement arc)
            {
                arcs.Add(arc);
				tg.arcnumber++;
            }

			public void AddZeroArc(NodeElement arc)
			{
				ZeroArc = arc;
				tg.arcnumber++;
			}

			public Node(TableGenerator tg, bool returnNode) 
			{ 
				this.returnNode = returnNode; 
				tg.nodelist.Add(this); 
				this.tg = tg;
			}

			public int Numerate(int start)
			// Returns number of arcs
			{
				for(int i=0; i<arcs.Count; i++)
					(arcs[i] as NodeElement).arcNumber = start+i;

				if(ZeroArc==null)
					count = arcs.Count;
				else
				{
					ZeroArc.arcNumber = arcs.Count;
					count = arcs.Count + 1;
				}
				return count;
			}
			
			public int Count()
			{
				return count;
			}

			public int FirstArcNumber()
			{
				if(returnNode) return 0;
				if(arcs.Count == 0) return 1; //throw new Exception("No arcs in non return node");
				return (arcs[0] as NodeElement).arcNumber;
			}

			public EBNFField GetEBNFField(int arcnumber)
			{
				if(arcnumber >= count) throw new Exception("GetEBNFField: wrong field number");
				if(arcnumber == arcs.Count)
					return ZeroArc.GetEBNFField(true);
				else
                    return (arcs[arcnumber] as NodeElement).GetEBNFField((arcnumber == arcs.Count - 1)&&(ZeroArc == null));
			}
        }

        /// <summary>
        /// TODO: refactoring
        /// This class together with node should be removed and a net based on the Arc class should be generated
        /// </summary>
        public class NodeElement
        {
            public int arcNumber = -1; // In table (line);
            // Table fields
            public int checkCode = -1; //Not set yet 
            public bool receive;  // Direction
            public bool terminal; //  
            public int nextNode = -1; //Not set yet

            // Temp fields
            public Node linkToNextNode = null;
            public Node linkToSubnetNode = null;

            public NodeElement(Node linkToNextNode, Node linkToSubnetNode, bool terminal, bool receive, int checkCode)
            {
                this.linkToNextNode = linkToNextNode;
                this.linkToSubnetNode = linkToSubnetNode;
                this.terminal = terminal;
                this.receive = receive;
                if(terminal) this.checkCode = checkCode;
            }

			public EBNFField GetEBNFField(bool lastInNode)
			{
				if(arcNumber == -1) throw new Exception("Internal error in EBNF generation: Numerate() shold be called first.");
				if(checkCode == -1) 
				{	
					if(!terminal)
					{
						checkCode = linkToSubnetNode.FirstArcNumber();
					}
					else
						throw new Exception("Internal error in EBNF generation: No checkCode for terminal arc");
				}

				if(nextNode == -1) 
				{
					nextNode = linkToNextNode.FirstArcNumber();
				}
				EBNFField ebnf = new EBNFField(checkCode, terminal, receive, nextNode, lastInNode);		
				return ebnf;
			}
        }
		
		public void AddNet(Node entry) 
		{
			nets.Add(entry);
		}        

		public bool validate()
		{
            // We have initial table at this point
            // Rules to check
            // 1. No recursion
            //    This is checked by parser through enforcing the order of declarations and their use
            // 2. All data transfer in nodes done in one direction
            
            // Get the tree
            Arc root = VisitNode(EBNF, new Dictionary<int, Arc>(), 0, false, true);
            List<Arc> allroots = new List<Arc>();
            int arcscount = FindRoots(root, allroots);
            bool err = false;
            foreach (Arc arc in allroots)
            {
                Arc cur = arc;
                bool dirset = false;
                bool receive = false;
                while (cur != null)
                {
                    if (cur.checkCode != -1)
                    {
                        if (dirset)
                        {
                            if (cur.receive != receive)
                            {
                                err = true; break;
                            }
                        }
                        else
                        {
                            receive = cur.receive;
                            dirset = true;
                        }
                    }
                    cur = cur.nextInTheNode;
                }
            }

			return !err;
        }

	
		// Applys numbers to arcs and links
		private void Numerate()
		{
			// Apply global numebers for arcs
			int arcnum = 1; // Starting number is 1 (0 is reserved)
			for(int i=0; i<nodelist.Count; i++)
			{
				arcnum += (nodelist[i] as Node).Numerate(arcnum);
			}
#if DEBUG	//Assert
			if(arcnum != arcnumber + 1) 
                throw new Exception("EBNF Generation: smth strange with number of arcs.");
#endif
		}

		// Convers to table
		private void BuildTable()
		{
            if(nets.Count == 0) return;
			EBNF = new EBNFField[ arcnumber + 1 ];
			EBNF[0] = new EBNFField(0, true, false, (nets[nets.Count - 1] as Node).FirstArcNumber(), true);
			int count = 1;
			for(int i=0; i<nodelist.Count; i++)
			{
				for(int j=0; j<(nodelist[i] as Node).Count(); j++)
				{
					EBNF[count] = (nodelist[i] as Node).GetEBNFField(j);
					count++;
				}
			}
			if(count != arcnumber + 1) throw new Exception("EBNF Generation: wrong number of fields in the EBNF table");
#if DEBUG_PROTOCOL
			System.Console.WriteLine("TABLE IS GENERATED");
			report();
#endif
		}

		public void Generate()
		{
			Numerate();
            BuildTable();
		}
		
		public TableGenerator() { }

#if DEBUG        
		public void report()
        {
            System.Console.WriteLine("Protocol table:");
			System.Console.WriteLine("#\t checkCode \t terminal \t receive \t next \t\t endNode");
            if (EBNF == null)
            {
                Console.WriteLine("NULL");
            }
            else
            {
                for (int i = 0; i < EBNF.Length; i++)
                {
                    System.Console.Write("{0}:\t", i); EBNF[i].report();
                }

                System.Console.WriteLine("DOT FILE TO DISPLAY GRAPH WITH GRAPHVIZ:");
                System.Console.WriteLine("Viewer can be obtaned from http://www.graphviz.org/");
                System.Console.WriteLine("digraph run {");
                System.Console.WriteLine("rankdir=LR;");
                // Create vertexes. Name it by entry lune to its bush
                int ends = 0;
                for (int i = 1; i < EBNF.Length; i++)
                {
                    if (EBNF[i].next == 0)
                    {
                        ends++;
                        System.Console.WriteLine("vE{0} [label=\"@\",shape=circle];",ends);
                    }
                    if(EBNF[i-1].endNode)
                        System.Console.WriteLine("v{0} [label=\"{0}\",shape=circle];", i); 
                }
                int currentBush = 0;
                ends = 0;
                for (int i = 1; i < EBNF.Length; i++)
                {
                    if (EBNF[i - 1].endNode) currentBush = i;
                    if (EBNF[i].next == 0)
                    {
                        ends++;
                        System.Console.WriteLine("v{0}->vE{1} [label=\"{5}: {4}{3}:{2}\"];", currentBush, ends, EBNF[i].checkCode, EBNF[i].terminal ? "check" : "goto", EBNF[i].receive ? "?" : "", i);
                    }else
                        System.Console.WriteLine("v{0}->v{1} [label=\"{5}: {4}{3}:{2}\"];", currentBush, EBNF[i].next, EBNF[i].checkCode, EBNF[i].terminal ? "check" : "goto", EBNF[i].receive ? "?" : "", i);
                }
                System.Console.WriteLine("}");
            }

		}
#endif

        class Arc
        {
            private const int END = -1;
            private const int ANY = -2;
            public int id;
            public int checkCode;
            public bool receive;
            public bool merged = false;

            public Arc nextNode;
            public Arc nextInTheNode;


            public bool IsEnd() { return checkCode == END; }

            public Arc(TableGenerator.EBNFField f)
            {
                checkCode = f.checkCode;
                Debug.Assert(f.terminal);
                receive = f.receive;
                nextInTheNode = null;
                nextNode = null;
            }

            public Arc(bool direction)
            {
                checkCode = ANY; // Nothing to check                
                receive = direction;
                nextInTheNode = null;
                nextNode = null;
            }

            private Arc() { }

            public Arc CloneNode()
            {
                Arc cl = new Arc();
                cl.checkCode = checkCode;
                cl.nextNode = nextNode;
                cl.receive = receive;
                if (nextInTheNode != null) cl.nextInTheNode = nextInTheNode.CloneNode();
                return cl;
            }

            public void Set(Arc other)
            {
                checkCode = other.checkCode;
                nextNode = other.nextNode;
                receive = other.receive;
                nextInTheNode = other.nextInTheNode;
                this.id = other.id;
            }

            public static Arc CreateEndArc()
            {
                Arc e = new Arc();
                e.nextNode = null;
                e.nextInTheNode = null;
                e.id = 0;
                e.receive = false;
                e.checkCode = END; // END code
                return e;
            }
        }

        private Arc VisitNode(
            TableGenerator.EBNFField[] EBNF,
            Dictionary<int, Arc> map,
            int node,
            bool samenode,
            bool zerolevel
            )
        {
            if (zerolevel && node == 0 && map.ContainsKey(0)) return Arc.CreateEndArc();
            if (map.ContainsKey(node)) return map[node];
            Arc root = null, prev = null;
            do
            {
                if (EBNF[node].terminal)
                {
                    Arc cur = new Arc(EBNF[node]);

                    if (root == null)
                    {
                        root = prev = cur;
                        if (!samenode)
                        {
                            map.Add(node, root);
                        }
                    }
                    else
                    {
                        prev.nextInTheNode = cur;
                        prev = cur;
                    }

                    if (EBNF[node].checkCode == 0)
                    {
                        // We need to prevent loops
                        if (root == null) map.Add(node, new Arc(EBNF[node]));
                        // This is a "skip" arc -> we need to copy arcs that follow it
                        Arc nextNode = VisitNode(EBNF, map, EBNF[node].next, false, zerolevel);
                        // Copy each of them in that list to this list
                        cur.Set(nextNode.CloneNode()); // We deepcopy the first using set
                    }
                    else
                    {
                        cur.nextNode = VisitNode(EBNF, map, EBNF[node].next, false, zerolevel);
                    }

                    do
                    {
                        cur.id = node;
                        if (cur.nextInTheNode == null) break;
                        else cur = cur.nextInTheNode;
                    } while (true);


                }
                else
                {
                    Arc next = VisitNode(EBNF, map, EBNF[node].next, false, zerolevel);
                    // Insert a subgraph
                    Dictionary<int, Arc> nmap = new Dictionary<int, Arc>();
                    nmap.Add(0, next); // Return value is 0 so this will make the return of the subgraph point to the continuation
                    Arc cur = VisitNode(EBNF, nmap, EBNF[node].checkCode, true, false);
                    if (root == null)
                    {
                        root = cur;
                        prev = root;
                        if (!samenode)
                        {
                            map.Add(node, root);
                        }
                    }
                    else
                    {
                        prev.nextInTheNode = cur;
                    }
                    while (prev.nextInTheNode != null) prev = prev.nextInTheNode;
                }
            } while (!EBNF[node++].endNode);
            return root;
        }

        private enum MergeType { Send, Receive };

        private Arc Merge(Arc root, MergeType merge)
        {
            // We have a rule that all sends within one node should be preformed in one direction
            // hence if we merge -> we merge the entire node. Special case is END arc which is never merged

            // If the node is to be merged then we create a new destination node for the merged set and copy all the
            // arcs to there. See documentation for more detail.
            if (root.merged) return root;
            root.merged = true;

            bool hasEndArc = false;
            Arc endArc = null;
            bool hasSend = false;
            bool hasReceive = false;

            Arc cur = root;
            int count = 0;
            while (cur != null)
            {
                if (cur.IsEnd()) { hasEndArc = true; endArc = cur; }
                if (cur.receive) hasReceive = true;
                else hasSend = true;
                cur = cur.nextInTheNode;
                count++;
            }
            if (hasSend && hasReceive)
            { // ERROR: Should be checked at eraly stage in validate()
                Debug.Assert(false);
                return root;
            }
            if ((merge == MergeType.Receive && hasSend) || (merge == MergeType.Send && hasReceive) || count == 1 || (count == 2 && hasEndArc))
            {
                // Nothing to do with this node. Try to merge all next nodes
                cur = root;
                while (cur != null)
                {
                    if (!cur.IsEnd()) { cur.nextNode = Merge(cur.nextNode, merge); }
                    cur = cur.nextInTheNode;
                }
                return root;
            }


            // Merge all except for End
            cur = root;
            Arc prev = null;
            root = new Arc(merge == MergeType.Receive);
            bool addEnd = false;
            while (cur != null)
            {
                // To the end of the arc add copies of outgoing edges from all merged arcs                
                if (!cur.IsEnd())
                {
                    Arc more = Merge(cur.nextNode, merge).CloneNode();
                    if (prev == null)
                    {
                        root.nextNode = more;
                        prev = more;
                    }
                    else
                    {
                        if (prev.IsEnd())
                        {
                            prev.Set(more);
                            addEnd = true;
                        }
                        else
                            prev.nextInTheNode = more;
                    }

                    while (prev.nextInTheNode != null)
                    {
                        prev = prev.nextInTheNode;
                    }
                }
                cur = cur.nextInTheNode;
            }

            // Add END edge in the very end if needed
            if (addEnd || hasEndArc) prev.nextInTheNode = Arc.CreateEndArc();
            return root;
        }

        private int FindRoots(Arc root, List<Arc> allroots)
        {
            if (allroots.Contains(root)) return 0;
            allroots.Add(root);
            Arc cur = root;
            int count = 0;
            while (cur != null)
            {
                count++;
                count += FindRoots(cur.nextNode, allroots);
                cur = cur.nextInTheNode;
            }
            return count;
        }

        private TableGenerator.SEBNF[] SubstituteRecursion(TableGenerator.EBNFField[] EBNF, MergeType merge, bool domerge)
        {
            Debug.Assert(EBNF.Length > 0);

            List<Arc> allroots = new List<Arc>();

            Arc root = VisitNode(EBNF, new Dictionary<int, Arc>(), 0, false, true);
            if(domerge) root = Merge(root, merge);
            int arcscount = FindRoots(root, allroots);

            TableGenerator.SEBNF[] nEBNF = new TableGenerator.SEBNF[arcscount];
            int node = 1;
            root.id = 0;
            for (int i = 1; i < allroots.Count; i++)
            {
                Arc cur = allroots[i];
                while (cur != null)
                {
                    cur.id = node;
                    node++;
                    cur = cur.nextInTheNode;
                }
            }
            Debug.Assert(node == arcscount);
            // Populate the table
            node = 0;
            for (int i = 0; i < allroots.Count; i++)
            {
                Arc cur = allroots[i];
                while (cur != null)
                {
                    nEBNF[node].checkCode = cur.checkCode;
                    nEBNF[node].receive = cur.receive;
                    if (cur.IsEnd())
                        nEBNF[node].next = 0;
                    else
                        nEBNF[node].next = cur.nextNode.id;
                    Debug.Assert(node == cur.id);
                    node++;
                    cur = cur.nextInTheNode;
                }
                nEBNF[node - 1].endNode = true;
            }
            return nEBNF;
        }

        SEBNF [] clientEBNF = null,
                 serverEBNF = null,
                 runtimeEBNF = null;

        public SEBNF[] GetClientEBNF()
        {
            if(clientEBNF == null) clientEBNF = SubstituteRecursion(EBNF, MergeType.Receive, true);
            return clientEBNF;
        }

        public SEBNF[] GetServerEBNF()
        {
            if(serverEBNF == null) serverEBNF = SubstituteRecursion(EBNF, MergeType.Send, true);
            return serverEBNF;
        }

        public SEBNF[] GetRuntimeEBNF()
        {
            if(runtimeEBNF == null) runtimeEBNF = SubstituteRecursion(EBNF, MergeType.Send, false);
            return runtimeEBNF;
        }
    }

    //////////////////////////////////////////////////////////////////////////////
    
    public abstract class EXTENSION : NODE
    {
        public EXTENSION (ASTNodeType astNodeType, Identifier name ) : base(astNodeType, name) { }

    //  --------------------------------------------------
        public override Node convert() { return null; }
        public override NODE resolve() { return null; }
        public override TYPE type { get { return null; } set { } }
        public override bool validate() { return true; }
    //  public override void report(int shift) { }
#if DEBUG
        public override void report_short() { }
#endif
        public override NODE find(Identifier name) { return null; }
    //  --------------------------------------------------

    }

    public sealed class SYNTAX : EXTENSION
    {
        public TableGenerator table;
        public TestFunctionGenerator testFunction;

        public PRODUCTION_LIST productions;
        public UNRESOLVED_LIST unresolved;  // unresolved terminals; potentially keywords

        public ENUM_TYPE keywords;  // reference to the type with keywords from the protocol

        public SYNTAX()
            : base(ASTNodeType.SYNTAX, null)
        {
            productions = new PRODUCTION_LIST();
            unresolved = new UNRESOLVED_LIST();

            table = null;
            testFunction = new TestFunctionGenerator();
        }

        public override NODE find(Identifier name)
        {
            for (int i = 0, n = productions.Length; i < n; i++)
                if (productions[i].name.Name == name.Name)
                    return productions[i];

            for (int i = 0, n = unresolved.Length; i < n; i++)
                if (unresolved[i].name.Name == name.Name)
                    return unresolved[i];

            return null;
        }

        public bool check()
        {
            if (productions.Length == 0)
            {
                ERROR.EmptyProtocol(enclosing.name.Name, sourceContext);
                this.ErrorReported = true;
            }
            for (int i = 0, n = unresolved.Length; i < n; i++)
            {
                UNKNOWN_NONTERMINAL unknown = unresolved[i];
                if (unknown.resolved == UNKNOWN_NONTERMINAL.Resolved.NotResolved)
                {
                    this.ErrorReported = true;
                    ERROR.UndeclaredProduction(unknown.name.Name, unknown.sourceContext);
                }
            }
            return !ErrorReported;
        }

        /* Summary:
         *   Convert protocol to data structures for future transformation
         *   and table generation
         * */
        private void generateTable()
        {
            if (table != null) return;
            table = new TableGenerator();
            for (int i = 0, n = productions.Length; i < n; i++)
            {
                TableGenerator.Node end = new TableGenerator.Node(table, true);
                productions[i].root = new TableGenerator.Node(table, false);
                productions[i].right_part.generate(table, testFunction, productions[i].root, end);
                table.AddNet(productions[i].root);
            }
            table.Generate();
        }

        public override bool validate()
        {
            if (!CONTEXT.firstPass)
            {
                generateTable();
                bool ok = check();
                // There is a stack overflow here. The algorithm needs to be rewritten correctly
                //if (ok && !ErrorReported)
                //{
                //    if (!table.validate())
                //    {
                //        if (!ErrorReported)
                //            ERROR.ProtocolAmbiguity(sourceContext, enclosing.Name);
                //        ErrorReported = true;
                //    }
                //}
                //else return false;

            }
            return true;
        }

        public override Node convert()
        {
            // Generates a "state machine table" which goes to the
            // protocol's constructor body.

            if (!ErrorReported) generateTable();

            return null;

        }
#if DEBUG
        public override void report(int shift)
        {
            if (productions == null || productions.Length == 0)
            {
                NODE.doShift(shift + NODE.reportShift);
                System.Console.WriteLine("MISSED");
                return;
            }

            for (int i = 0, n = productions.Length; i < n; i++)
                productions[i].report(shift);

            testFunction.report();
            if (table == null)
            {
                NODE.doShift(shift + NODE.reportShift);
                System.Console.WriteLine("MISSED");
            }
            else
                table.report();
        }
#endif
    }

        public sealed class PRODUCTION : EXTENSION
        {
            public SYNTAX syntax;  // just a reference to the enclosing syntax
            public UNIT right_part;
            public TableGenerator.Node root = null;

            public PRODUCTION(Identifier name, SYNTAX s)
                : base(ASTNodeType.PRODUCTION, name)
            {
                syntax = s;

                // Check for duplication
                if (syntax.productions.find(name) != null)
                    ERROR.DuplicateDeclaration(name.SourceContext, name.Name);

                // Take the name of the new production and check if it is 
                // in the list of unresolved. If so, resolve it.
                UNKNOWN_NONTERMINAL unknown = syntax.unresolved.find(name);
                if (unknown != null)
                {
                    while (unknown != null)
                    {
                        unknown.resolved = UNKNOWN_NONTERMINAL.Resolved.ResolvedAsProduction;
                        unknown.resolved_as_production = this;
                        unknown = unknown.previous;
                    }
                }

                // And finally, add new production to the syntax.
                syntax.productions.Add(this);
            }


#if DEBUG
            public override void report(int shift)
            {
                NODE.doShift(shift);
                System.Console.Write("{0} = ", name);
                right_part.report(0);
                System.Console.WriteLine(";");
            }
#endif

        }

        // UNIT: Common class to all kinds of entities appearing
        //       in the right hand sides of productions.
        //
        public abstract class UNIT : EXTENSION
        {
            public UNIT(ASTNodeType astNodeType) : base(astNodeType, null) { }

            public enum Kind { Always, Optional, ZeroOrMore };
            public Kind kind;

            public void reportLeftParenth(bool withParenths)
            {
                switch (kind)
                {
                    case Kind.Always: if (withParenths) System.Console.Write("( "); break;
                    case Kind.Optional: System.Console.Write("[ "); break;
                    case Kind.ZeroOrMore: System.Console.Write("{ "); break;
                }
            }

            public void reportRightParenth(bool withParenths)
            {
                switch (kind)
                {
                    case Kind.Always: if (withParenths) System.Console.Write(")"); break;
                    case Kind.Optional: System.Console.Write("]"); break;
                    case Kind.ZeroOrMore: System.Console.Write("}"); break;
                }
            }

            protected TableGenerator.Node preGenerate(TableGenerator tg, TestFunctionGenerator tfg, TableGenerator.Node first, TableGenerator.Node last)
            // Returns the new last node for futher generated arcs
            {
                if (kind == Kind.ZeroOrMore || kind == Kind.Optional)
                    first.AddZeroArc(new TableGenerator.NodeElement(last, null, true, false, 0));

                if (kind == Kind.ZeroOrMore)
                    return first; // Create a cycle
                else
                    return last;
            }

            public abstract void generate(TableGenerator tg, TestFunctionGenerator tfg, TableGenerator.Node first, TableGenerator.Node last);
        }

        // TERMINAL: The "true" terminal symbol - one from those listed
        //           in the beginning of the protocol declaration.
        //
        public sealed class TERMINAL : UNIT
        {
            public ENUMERATOR_DECL enumerator;
            public bool receive;  // for '?' sign

            public TERMINAL(ENUMERATOR_DECL e, bool r)
                : base(ASTNodeType.TERMINAL)
            {
                enumerator = e;
                receive = r;
            }

#if DEBUG
            public override void report(int shift)
            {
                if (receive) System.Console.Write("?");
                System.Console.Write("{0}", enumerator.name.Name);
            }
#endif
            public override void generate(TableGenerator tg, TestFunctionGenerator tfg, TableGenerator.Node first, TableGenerator.Node last)
            {
                int code = tfg.RegisterTerminalTest(enumerator);
                TableGenerator.NodeElement arc = new TableGenerator.NodeElement(preGenerate(tg, tfg, first, last), null, true, receive, code);
                first.AddArc(arc);
            }
        }

        // TYPE_NAME: A type name declared somewhere outside of the protocol.
        //            The type denoted by the type name should be one
        //            of the following: integer/cardinal/real/char/string.
        //
        public sealed class TYPE_NAME : UNIT
        {
            public TYPE type_name;
            public bool receive;  // for '?' sign

            public TYPE_NAME(TYPE t, bool r) : base(ASTNodeType.TYPE_NAME) { type_name = t; receive = r; }

#if DEBUG
            public override void report(int shift)
            {
                if (receive) System.Console.Write("?");
                System.Console.Write("{0}", type_name.enclosing.name.Name);
            }
#endif

            public override void generate(TableGenerator tg, TestFunctionGenerator tfg, TableGenerator.Node first, TableGenerator.Node last)
            {
                int code = tfg.RegisterTypeTest(type_name);
                TableGenerator.NodeElement arc = new TableGenerator.NodeElement(preGenerate(tg, tfg, first, last), null, true, receive, code);
                first.AddArc(arc);
            }
        }

        // CONSTANT: A name of a constant declared somewhere outside
        //           of the protocol.
        //
        public sealed class CONSTANT : UNIT
        {
            public LITERAL literal;
            public bool receive;  // for '?' sign

            public CONSTANT(LITERAL l, bool r) : base(ASTNodeType.CONSTANT) { literal = l; receive = r; }

#if DEBUG
            public override void report(int shift)
            {
                if (receive) System.Console.Write("?");
                System.Console.Write("{0}", literal.calculate());
            }
#endif

            public override void generate(TableGenerator tg, TestFunctionGenerator tfg, TableGenerator.Node first, TableGenerator.Node last)
            {
                int code = tfg.ReqisterConstantTest(literal);
                TableGenerator.NodeElement arc = new TableGenerator.NodeElement(preGenerate(tg, tfg, first, last), null, true, receive, code);
                first.AddArc(arc);
            }
        }

        // NONTERMINAL: A name of a production introduced somewhere
        //              in the protocol.
        //
        public sealed class NONTERMINAL : UNIT
        {
            public PRODUCTION production;

            public NONTERMINAL(PRODUCTION prod) : base(ASTNodeType.NONTERMINAL) { production = prod; }

#if DEBUG
            public override void report(int shift)
            {
                System.Console.Write("{0}", production.name.Name);
            }
#endif
            public override void generate(TableGenerator tg, TestFunctionGenerator tfg, TableGenerator.Node first, TableGenerator.Node last)
            {
                TableGenerator.NodeElement arc = new TableGenerator.NodeElement(preGenerate(tg, tfg, first, last), production.root, false, false, 0);
                first.AddArc(arc);
            }

        }

        // UNKNOWN_NONTERMINAL: An unresolved production name.
        //                      This is a completely internal node;
        //                      shouldn't appear in a correctly defined grammar.
        //
        [Obsolete]
        public sealed class UNKNOWN_NONTERMINAL : UNIT
        {
            public enum Resolved { NotResolved, ResolvedAsProduction, ResolvedAsType };
            public UNKNOWN_NONTERMINAL previous;
            public Resolved resolved;
            public PRODUCTION resolved_as_production;
            public TYPE resolved_as_type;
            public bool receive;  // for '?' sign

            public UNKNOWN_NONTERMINAL(SYNTAX syntax, Identifier id, bool rec)
                : base(ASTNodeType.UNKNOWN_NONTERMINAL)
            {
                Debug.Assert(false); // We enforce order to prevent recursion. 

                UNKNOWN_NONTERMINAL prev = syntax.unresolved.find(id);

                name = id; resolved = Resolved.NotResolved; receive = rec;

                if (prev != null)
                {
                    this.previous = prev.previous;
                    prev.previous = this;
                }
                else
                {
                    // If this is the first unresolved with the given name,
                    // add it to the list of unresolved.
                    syntax.unresolved.Add(this);
                }
            }

#if DEBUG
            public override void report(int shift)
            {
                System.Console.Write(this.name.Name);
            }
#endif

            public override void generate(TableGenerator tg, TestFunctionGenerator tfg, TableGenerator.Node first, TableGenerator.Node last)
            {
                if (resolved == Resolved.ResolvedAsProduction)
                {
                    TableGenerator.NodeElement arc = new TableGenerator.NodeElement(preGenerate(tg, tfg, first, last), resolved_as_production.root, false, false, 0);
                    first.AddArc(arc);
                }
                else if (resolved == Resolved.ResolvedAsType)
                {
                    int code = tfg.RegisterTypeTest(resolved_as_type);
                    TableGenerator.NodeElement arc = new TableGenerator.NodeElement(preGenerate(tg, tfg, first, last), null, true, receive, code);
                    first.AddArc(arc);
                }
                else throw new Exception("Never should be called");
            }
        }

        // SEQUENCE: Something like this:
        //
        //           P = A { B } C ( X | Y )
        //               ^^^^^^^^^^^^^^^^^^^
        // This sequence contains four units.
        //
        public sealed class SEQUENCE : UNIT
        {
            public UNIT_LIST sequence;

            public SEQUENCE()
                : base(ASTNodeType.SEQUENCE)
            {
                sequence = new UNIT_LIST();
            }

#if DEBUG
            public override void report(int shift)
            {
                if (sequence == null)
                {
                    System.Console.Write("EMPTY");
                    return;
                }
                int L = sequence.Length;
                bool withParenths = (L > 1) || !(this.enclosing is PRODUCTION);

                base.reportLeftParenth(withParenths);
                for (int i = 0; i < L; i++)
                {
                    sequence[i].report(0);
                    System.Console.Write(" ");
                }
                base.reportRightParenth(withParenths);
            }
#endif
            public override void generate(TableGenerator tg, TestFunctionGenerator tfg, TableGenerator.Node first, TableGenerator.Node last)
            {
                int L = sequence.Length;
                TableGenerator.Node t_begin = first;
                for (int i = 0; i < L - 1; i++)
                {
                    TableGenerator.Node t_end = new TableGenerator.Node(tg, false);
                    sequence[i].generate(tg, tfg, t_begin, t_end);
                    t_begin = t_end;
                }
                sequence[L - 1].generate(tg, tfg, t_begin, preGenerate(tg, tfg, first, last));
            }
        }

        // ALTERNATIVES: Something like this:
        //
        // P = A B | C D | X
        //     ^^^   ^^^   ^  - These are alternatives
        //
        public sealed class ALTERNATIVES : UNIT
        {
            public UNIT_LIST alternatives;

            public ALTERNATIVES()
                : base(ASTNodeType.ALTERNATIVES)
            {
                alternatives = new UNIT_LIST();
            }

#if DEBUG
            public override void report(int shift)
            {
                int L = alternatives.Length;
                bool withParenths = !(this.enclosing is PRODUCTION);

                base.reportLeftParenth(withParenths);
                for (int i = 0; i < L; i++)
                {
                    alternatives[i].report(0);
                    if (i < L - 1) System.Console.Write(" | ");
                }
                base.reportRightParenth(withParenths);
            }
#endif
            public override void generate(TableGenerator tg, TestFunctionGenerator tfg, TableGenerator.Node first, TableGenerator.Node last)
            {
                int L = alternatives.Length;
                TableGenerator.Node nlast = preGenerate(tg, tfg, first, last);
                for (int i = 0; i < L; i++)
                {
                    alternatives[i].generate(tg, tfg, first, nlast);
                }
            }
        }

        public sealed class PRODUCTION_LIST  ////////////////////////////////
        {
            private PRODUCTION[] productions;
            private int length = 0;

            public PRODUCTION_LIST() { this.productions = new PRODUCTION[8]; }
            public PRODUCTION_LIST(int n) { this.productions = new PRODUCTION[n]; }

            public void Add(PRODUCTION prd)
            {
                int n = this.productions.Length;
                int i = this.length++;
                if (i == n)
                {
                    PRODUCTION[] new_productions = new PRODUCTION[n + 8];
                    for (int j = 0; j < n; j++) new_productions[j] = productions[j];
                    this.productions = new_productions;
                }
                this.productions[i] = prd;
            }

            public int Length
            {
                get { return this.length; }
                set { this.length = value; }
            }

            public PRODUCTION this[int index]
            {
                get { return this.productions[index]; }
                set { this.productions[index] = value; }
            }

            public PRODUCTION find(PRODUCTION prod)
            {
                PRODUCTION result = null;
                for (int i = 0, n = Length; i < n; i++)
                {
                    if (productions[i] == prod) { result = productions[i]; break; }
                }
                return result;
            }

            public PRODUCTION find(Identifier name)
            {
                PRODUCTION result = null;
                for (int i = 0, n = Length; i < n; i++)
                {
                    Identifier prod_name = productions[i].name;
                    if (prod_name == null) continue;
                    if (prod_name.Name == name.Name) { result = productions[i]; break; }
                }
                return result;
            }
        }

        public sealed class UNIT_LIST  ////////////////////////////////
        {
            private UNIT[] units;
            private int length = 0;

            public UNIT_LIST() { this.units = new UNIT[8]; }
            public UNIT_LIST(int n) { this.units = new UNIT[n]; }

            public void Add(UNIT unit)
            {
                int n = this.units.Length;
                int i = this.length++;
                if (i == n)
                {
                    UNIT[] new_units = new UNIT[n + 8];
                    for (int j = 0; j < n; j++) new_units[j] = units[j];
                    this.units = new_units;
                }
                this.units[i] = unit;
            }

            public int Length
            {
                get { return this.length; }
                set { this.length = value; }
            }

            public UNIT this[int index]
            {
                get { return this.units[index]; }
                set { this.units[index] = value; }
            }

            public UNIT find(UNIT unit)
            {
                UNIT result = null;
                for (int i = 0, n = Length; i < n; i++)
                {
                    if (units[i] == unit) { result = units[i]; break; }
                }
                return result;
            }

            public UNIT find(Identifier name)
            {
                UNIT result = null;
                for (int i = 0, n = Length; i < n; i++)
                {
                    Identifier unit_name = units[i].name;
                    if (unit_name == null) continue;
                    if (unit_name.Name == name.Name) { result = units[i]; break; }
                }
                return result;
            }
        }

        public sealed class UNRESOLVED_LIST  ////////////////////////////////
        {
            private UNKNOWN_NONTERMINAL[] unresolved;
            private int length = 0;

            public UNRESOLVED_LIST() { this.unresolved = new UNKNOWN_NONTERMINAL[8]; }
            public UNRESOLVED_LIST(int n) { this.unresolved = new UNKNOWN_NONTERMINAL[n]; }

            public void Add(UNKNOWN_NONTERMINAL unknown)
            {
                int n = this.unresolved.Length;
                int i = this.length++;
                if (i == n)
                {
                    UNKNOWN_NONTERMINAL[] new_unresolved = new UNKNOWN_NONTERMINAL[n + 8];
                    for (int j = 0; j < n; j++) new_unresolved[j] = unresolved[j];
                    this.unresolved = new_unresolved;
                }
                this.unresolved[i] = unknown;
            }

            public int Length
            {
                get { return this.length; }
                set { this.length = value; }
            }

            public UNKNOWN_NONTERMINAL this[int index]
            {
                get { return this.unresolved[index]; }
                set { this.unresolved[index] = value; }
            }

            public UNKNOWN_NONTERMINAL find(UNKNOWN_NONTERMINAL unknown)
            {
                UNKNOWN_NONTERMINAL result = null;
                for (int i = 0, n = Length; i < n; i++)
                {
                    if (unresolved[i] == unknown) { result = unresolved[i]; break; }
                }
                return result;
            }

            public UNKNOWN_NONTERMINAL find(Identifier name)
            {
                UNKNOWN_NONTERMINAL result = null;
                for (int i = 0, n = Length; i < n; i++)
                {
                    Identifier unknown_name = unresolved[i].name;
                    if (unknown_name == null) continue;
                    if (unknown_name.Name == name.Name) { result = unresolved[i]; break; }
                }
                return result;
            }
        }
    }

