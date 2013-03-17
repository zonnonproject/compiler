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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Compiler;
using System.Xml;
using System.Diagnostics;

namespace ETH.Zonnon{

    public class GNode
    {
        public readonly NODE astNode;
        public bool visited;

        public Dictionary<int, HashSet<int>> set = new Dictionary<int, HashSet<int>>();

        public HashSet<int> GetStatesFor(int pid)
        {
            if (!set.ContainsKey(pid)) set[pid] = new HashSet<int>();
            return set[pid];
        }

        public void ForwardAllStatesTo(GNode next)
        {
            foreach (int pid in set.Keys)
            {
                foreach (int state in set[pid])
                {
                    next.AddStateFor(pid, state);
                }
            }
        }

        public void ForwardAllStatesExceptOneTo(GNode next, int exceptionpid)
        {
            foreach (int pid in set.Keys)
            {
                if (pid == exceptionpid) continue;
                foreach (int state in set[pid])
                {
                    next.AddStateFor(pid, state);
                }
            }
        }

        public void AddStateFor(int pid, int state)
        {
            if (!set.ContainsKey(pid)) set[pid] = new HashSet<int>();
            if (!set[pid].Contains(state)) visited = false;
            set[pid].Add(state);
        }
        public LinkedList<GNode> successors;        

        public GNode(NODE node, GNode predecessor, SCFGBuilder builder)
        {            
            this.astNode = node;            
            successors = new LinkedList<GNode>();
            if(predecessor!=null) predecessor.successors.AddLast(this);
            builder.allNodes.AddLast(this);
        }
    }

    public sealed class ProtocolValidator
    {
        SCFGBuilder scfg;
        PROTOCOL_DECL aprot;
        public ProtocolValidator(SCFGBuilder scfg, PROTOCOL_DECL aprot)
        {
            this.scfg = scfg;
            this.aprot = aprot;
        }

        private bool Test(int acode, EXPRESSION obj, List<TestFunctionGenerator.Test> tests, bool strict)
        {
            int code = acode - 1;
            // For send constants and terminal symbols are strict
            if (tests[code].type == TestFunctionGenerator.TestType.ConstantTest)
            {
                if (strict) //Strict 
                {
                    if ((obj is LITERAL) && (((LITERAL)obj).val == tests[code].literal.val))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {   // There should be a type we can assign
                    return TYPE.AssignmentCompatibilityS(obj.type, tests[code].literal.type);
                }
            }
            else if (tests[code].type == TestFunctionGenerator.TestType.TypeTest)
            {
                if (strict) //Strict 
                {
                    return TYPE.AssignmentCompatibilityS(tests[code].type_name, obj.type);
                }
                else
                {
                    if (obj.Name == "#unused") return true;
                    return TYPE.AssignmentCompatibilityS(obj.type, tests[code].type_name);
                }
            }else if(tests[code].type == TestFunctionGenerator.TestType.TerminalTest){
                if (strict) //Strict 
                {
                    if ((obj is SELECTOR) && (((SELECTOR)obj).member == tests[code].enumerator))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {   // There should be a type we can assign
                    if (obj.Name == "#unused") return true;
                    return TYPE.AssignmentCompatibilityS(obj.type, tests[code].enumerator.type);
                }
            }

            return true;
        }

        private bool ValidateEnd(Rules rules, int currentArc)
        {
            TableGenerator.SEBNF[] EBNF = rules.EBNF;
            if (currentArc < 0 || EBNF == null) return false;
            
            if (EBNF[currentArc].checkCode == -1) return true;
            else return false;
        }
        private int ValidateDataTransfer(Rules rules, int currentArc, bool serverTOclient, EXPRESSION obj, bool strict)
        {
            TableGenerator.SEBNF[] EBNF = rules.EBNF;
            List<TestFunctionGenerator.Test> tests = rules.tests;

            if (currentArc < 0 || EBNF == null) return -1;

            // Trying to pass
            bool passed = true;
            do
            {
                passed = false;                
                
                    if ((EBNF[currentArc].receive == serverTOclient) &&
                        (((EBNF[currentArc].checkCode > 0) &&
                        Test(EBNF[currentArc].checkCode, obj, tests, strict)) 
                        ||
                        (EBNF[currentArc].checkCode == -2))
                        )                    
                    {
                        passed = true;
                        // Go to the next node
                        currentArc = EBNF[currentArc].next;
                    }
                    else if ((EBNF[currentArc].checkCode == -1) || EBNF[currentArc].endNode)
                    {
                        return -1;
                    }
                    else
                    {
                        currentArc++;
                    }                
            } while (!passed);
            return currentArc;
        }

        class Rules
        {
            public TableGenerator.SEBNF[] EBNF;
            public List<TestFunctionGenerator.Test> tests;
            public Stack<int> RKAP;
            public bool errorReported;
            public int id;
            public static int count = 0;
            public string name;
            public Rules(TableGenerator.SEBNF[] EBNF, List<TestFunctionGenerator.Test> tests, string name)
            {
                this.errorReported = false;
                this.name = name;
                this.EBNF = EBNF;
                this.tests = tests;
                this.id = ++count;
                RKAP = new Stack<int>();                
            }
            public string ExpectedAtState(int state, bool client)
            {
                int i = state;
                if(i<0||i>=EBNF.Length) return "";
                string expected = "";
                do {                    
                        if (expected.Length > 0) expected += ", ";
                        if (client)
                        {
                            if (EBNF[i].receive) expected += "receive "; else expected += "send ";
                        }
                        else
                        {
                            if (EBNF[i].receive) expected += "return "; else expected += "accept ";
                        }
                        if (EBNF[i].checkCode>0)
                            expected += tests[EBNF[i].checkCode-1].ToString();
                        else
                            expected += "anything";
                    if (EBNF[i].endNode) break;
                    i++;
                } while (true);
                return expected;
            }
        }

        Dictionary<int, Rules> ptotable = new Dictionary<int, Rules>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scfg"></param>
        /// <param name="aprot">If called for an agent activity - implemented protocol, otherwise null</param>
        /// <returns></returns>
        public bool Validate(SourceContext defaultContext)
        {
            HashSet<int> exclude = new HashSet<int>();
            foreach (GNode g in scfg.allNodes) g.visited = false;
            
            if (aprot != null && !aprot.ErrorReported)
            {
                aprot.syntax.validate(); aprot.syntax.convert();
                if (aprot.syntax.table != null) {
                    ptotable[0] = new Rules(aprot.syntax.table.GetServerEBNF(), aprot.syntax.testFunction.tests, "("+aprot.Name+") anonymous client");
                    scfg.entry.AddStateFor(ptotable[0].id, 0); //Entry point
                }
            }
            Queue<GNode> visitNext = new Queue<GNode>();
            visitNext.Enqueue(scfg.entry);            
            GNode node = scfg.entry;
            while (visitNext.Count > 0)
            {
                node = visitNext.Dequeue();
                node.visited = true;
                foreach (GNode next in node.successors)
                {
                    bool needtocopy = true;
                    if (next.astNode is ASSIGNMENT)
                    { // Interested in p := new A;
                        ASSIGNMENT nnode = next.astNode as ASSIGNMENT;
                        if (nnode.right_part is NEW && nnode.right_part.type is ACTIVITY_TYPE)
                        {
                            
                            if(nnode.receiver is INSTANCE){
                                int id = ((INSTANCE)nnode.receiver).entity.unique;
                            
                                ACTIVITY_TYPE acttype = nnode.right_part.type as ACTIVITY_TYPE;
                                if (((ACTIVITY_DECL)(acttype.activity)).prototype is PROTOCOL_DECL)
                                {
                                    if (!((ACTIVITY_DECL)(acttype.activity)).prototype.ErrorReported)
                                    {
                                        PROTOCOL_DECL eprot = ((ACTIVITY_DECL)(acttype.activity)).prototype as PROTOCOL_DECL;
                                        eprot.syntax.validate(); eprot.syntax.convert();
                                        if (eprot.syntax.table != null)
                                        {
                                            ptotable[id] = new Rules(eprot.syntax.table.GetClientEBNF(), eprot.syntax.testFunction.tests, "(" + eprot.name + ")" + ((INSTANCE)nnode.receiver).Name);
                                            next.AddStateFor(ptotable[id].id, 0);
                                            node.ForwardAllStatesExceptOneTo(next, ptotable[id].id);
                                            needtocopy = false;
                                        }
                                    }
                                }
                                else
                                { // Else it's ok. Do not trace this variable
                                    // Add to exclude set. All other cases will be treated with error: Not initialized dialog
                                    exclude.Add(id);
                                }
                            }else{
                                if(!nnode.ErrorReported)
                                    ERROR.ActivityVariableMustBeLocal(nnode.sourceContext);
                                nnode.ErrorReported = true;
                            }
                        }
                        else if ((nnode.right_part is CALL) &&
                            (((CALL)nnode.right_part).callee is INSTANCE && (((CALL)nnode.right_part).type is ABSTRACT_ACTIVITY_TYPE || ((CALL)nnode.right_part).type is ACTIVITY_TYPE))
                            )
                        {
                            int id = ((INSTANCE)((CALL)nnode.right_part).callee).entity.unique;
                            if (!exclude.Contains(id))
                            {
                                if (ptotable.ContainsKey(id))
                                {
                                    Rules rules = ptotable[id];
                                    foreach (int protocolState in node.GetStatesFor(rules.id))
                                    {
                                        int nextstate = protocolState;
                                        for (int i = 0; i < ((CALL)nnode.right_part).arguments.Length; i++)
                                        {
                                            EXPRESSION des = ((CALL)nnode.right_part).arguments[i];
                                            int savestate = nextstate;
                                            if ((nextstate = ValidateDataTransfer(rules, nextstate, false, des, true)) < 0)
                                            {
                                                if (!next.astNode.ErrorReported && !rules.errorReported)
                                                {
                                                    ERROR.ProtocolViolation(next.astNode.sourceContext, rules.ExpectedAtState(savestate, true));
                                                    next.astNode.ErrorReported = true;
                                                    rules.errorReported = true;
                                                }                                                                                                
                                            }
                                            else if (nextstate == 0) // This was the last send/receive
                                            {
                                                nextstate = -1;
                                            }                                            
                                        }
                                        { /* Process the receive */
                                            EXPRESSION des = nnode.receiver;
                                            int savestate = nextstate;
                                            if ((nextstate = ValidateDataTransfer(rules, nextstate, true, des, false)) < 0)
                                            {
                                                if (!next.astNode.ErrorReported && !rules.errorReported)
                                                {
                                                    ERROR.ProtocolViolation(next.astNode.sourceContext, rules.ExpectedAtState(savestate, true));
                                                    next.astNode.ErrorReported = true;
                                                    rules.errorReported = true;
                                                }                                                
                                            }
                                            else if (nextstate == 0) // This was the last send/receive
                                            {
                                                nextstate = -1;
                                            }
                                        }

                                        next.AddStateFor(rules.id, nextstate);
                                        node.ForwardAllStatesExceptOneTo(next, rules.id);
                                        needtocopy = false;
                                    }
                                }
                                else
                                {
                                    ERROR.AcceptCalledForInvalidDialog(nnode.sourceContext);
                                }
                            }
                        }
                    }
                    else if (next.astNode is ACCEPT)
                    {
                        ACCEPT anode = next.astNode as ACCEPT;
                        if (ptotable.ContainsKey(0))
                        {
                            Rules rules = ptotable[0];
                            foreach (int protocolState in node.GetStatesFor(rules.id))
                            {
                                int nextstate = protocolState;
                                for (int i = 0; i < anode.designators.Length; i++)
                                {
                                    EXPRESSION des = anode.designators[i];
                                    int savestate = nextstate;
                                    if ((nextstate = ValidateDataTransfer(rules, nextstate, false, des , false)) < 0)
                                    {
                                        if (!next.astNode.ErrorReported && !rules.errorReported)
                                        {
                                            ERROR.ProtocolViolation(next.astNode.sourceContext, rules.ExpectedAtState(savestate, false));
                                            next.astNode.ErrorReported = true;
                                            rules.errorReported = true;
                                        }                                        
                                    }
                                    else if (nextstate == 0) // This was the last send/receive
                                    {
                                        nextstate = -1;
                                    }

                                }
                                next.AddStateFor(rules.id, nextstate);
                                node.ForwardAllStatesExceptOneTo(next, rules.id);
                                needtocopy = false;
                            }
                        }
                    }
                    else if (next.astNode is REPLY)
                    {
                        REPLY rnode = next.astNode as REPLY;
                        if (ptotable.ContainsKey(0))
                        {
                            Rules rules = ptotable[0];
                            foreach (int protocolState in node.GetStatesFor(rules.id))
                            {
                                int nextstate = protocolState;
                                for (int i = 0; i < rnode.values_to_reply.Length; i++)
                                {
                                    EXPRESSION des = rnode.values_to_reply[i];
                                    int savestate = nextstate;
                                    if ((nextstate = ValidateDataTransfer(rules, nextstate, true, des, true)) < 0)
                                    {
                                        if (!next.astNode.ErrorReported && !rules.errorReported)
                                        {
                                            ERROR.ProtocolViolation(next.astNode.sourceContext, rules.ExpectedAtState(savestate, false));
                                            next.astNode.ErrorReported = true;
                                            rules.errorReported = true;
                                        }                                        
                                    }
                                    else if (nextstate == 0) // This was the last send/receive
                                    {
                                        nextstate = -1;
                                    }

                                }
                                next.AddStateFor(rules.id, nextstate);
                                node.ForwardAllStatesExceptOneTo(next, rules.id);
                                needtocopy = false;
                            }
                        }
                    }
                    else if (next.astNode is SEND_RECEIVE)
                    {
                        SEND_RECEIVE snode = next.astNode as SEND_RECEIVE;
                        if (snode.call.call.callee is INSTANCE)
                        {
                            int id = ((INSTANCE)snode.call.call.callee).entity.unique;
                            if (!exclude.Contains(id))
                            {
                                if (ptotable.ContainsKey(id))
                                {
                                    Rules rules = ptotable[id];
                                    foreach (int protocolState in node.GetStatesFor(rules.id))
                                    {
                                        int nextstate = protocolState;
                                        if (snode.call.call != null)
                                        {
                                            for (int i = 0; i < snode.call.call.arguments.Length; i++)
                                            {
                                                EXPRESSION des = snode.call.call.arguments[i];
                                                int savestate = nextstate;
                                                if ((nextstate = ValidateDataTransfer(rules, nextstate, false, des, true)) < 0)
                                                {
                                                    if (!next.astNode.ErrorReported&&!rules.errorReported)
                                                    {
                                                        ERROR.ProtocolViolation(next.astNode.sourceContext, rules.ExpectedAtState(savestate, true));
                                                        next.astNode.ErrorReported = true;
                                                        rules.errorReported = true;
                                                    }
                                                }
                                                else if (nextstate == 0) // This was the last send/receive
                                                {
                                                    nextstate = -1;
                                                }

                                            }
                                        }
                                        if (snode.leftParts != null)
                                        {
                                            for (int i = 0; i < snode.leftParts.Length; i++)
                                            {
                                                EXPRESSION des = snode.leftParts[i];
                                                int savestate = nextstate;
                                                if ((nextstate = ValidateDataTransfer(rules, nextstate, true, des, false)) < 0)
                                                {
                                                    if (!next.astNode.ErrorReported && !rules.errorReported)
                                                    {
                                                        ERROR.ProtocolViolation(next.astNode.sourceContext, rules.ExpectedAtState(savestate, true));
                                                        next.astNode.ErrorReported = true;
                                                        rules.errorReported = true;
                                                    }                                                    
                                                }
                                                else if (nextstate == 0) // This was the last send/receive
                                                {
                                                    nextstate = -1;
                                                }

                                            }
                                        }
                                        next.AddStateFor(rules.id, nextstate);
                                        node.ForwardAllStatesExceptOneTo(next, rules.id);
                                        needtocopy = false;
                                    }
                                }
                                else
                                {
                                    ERROR.AcceptCalledForInvalidDialog(snode.sourceContext);                                    
                                }
                            }
                        }
                        else
                        {
                            if (!snode.ErrorReported)
                                ERROR.ActivityVariableMustBeLocal(snode.sourceContext);
                            snode.ErrorReported = true;
                        }
                    }
                    else if (next.astNode is CALL_STMT)
                    {
                        CALL_STMT cnode = next.astNode as CALL_STMT;
                        if (cnode.call.callee is INSTANCE && (cnode.call.type is ABSTRACT_ACTIVITY_TYPE || cnode.call.type is ACTIVITY_TYPE))
                        {
                            int id = ((INSTANCE)cnode.call.callee).entity.unique;
                            if (!exclude.Contains(id))
                            {
                                if (ptotable.ContainsKey(id))
                                {
                                    Rules rules = ptotable[id];
                                    foreach (int protocolState in node.GetStatesFor(rules.id))
                                    {
                                        int nextstate = protocolState;
                                        if (cnode.call != null)
                                        {
                                            for (int i = 0; i < cnode.call.arguments.Length; i++)
                                            {
                                                EXPRESSION des = cnode.call.arguments[i];
                                                int savestate = nextstate;
                                                if ((nextstate = ValidateDataTransfer(rules, nextstate, false, des, true)) < 0)
                                                {
                                                    if (!next.astNode.ErrorReported && !rules.errorReported)
                                                    {
                                                        ERROR.ProtocolViolation(next.astNode.sourceContext, rules.ExpectedAtState(savestate, true));
                                                        next.astNode.ErrorReported = true;
                                                        rules.errorReported = true;
                                                    }
                                                }
                                                else if (nextstate == 0) // This was the last send/receive
                                                {
                                                    nextstate = -1;
                                                }

                                            }
                                        }

                                        next.AddStateFor(rules.id, nextstate);
                                        node.ForwardAllStatesExceptOneTo(next, rules.id);
                                        needtocopy = false;
                                    }
                                }
                            }
                            else
                            {
                                ERROR.AcceptCalledForInvalidDialog(cnode.sourceContext);                                
                            }
                        }
                    }

                    if (needtocopy) node.ForwardAllStatesTo(next);

                    if (!next.visited) visitNext.Enqueue(next);
                }
            }
            // We leave the graph. All protocols in the last node must be in -1 state

            foreach (Rules rule in ptotable.Values)
            {
                foreach (int protocolState in node.GetStatesFor(rule.id))
                {
                    if (!ValidateEnd(rule, protocolState) && !rule.errorReported)
                    {
                        ERROR.ProtocolNotCompleted(new SourceContext(defaultContext.Document, defaultContext.EndPos - 3, defaultContext.EndPos), rule.name, rule.ExpectedAtState(protocolState, true));
                    }
                }
            }
            return true;
        }
    }

    public sealed class SCFGBuilder : ASTVisitor
    {
        public GNode entry;
        public GNode last;
        public LinkedList<GNode> allNodes;
        private LinkedList<GNode> returns;
        private LinkedList<GNode> breaks;

        public override string ToString()
        {
            string cfg = "digraph G {\n";
            foreach (GNode g in allNodes)
            {
                /*
                          "Block0" [
                            label = "Block #0\lEntry\l\l"
                            shape = "box"
                          ];                 
                 */
                string name = ((g.astNode == null) ? g.GetHashCode().ToString() : g.astNode.sourceContext.SourceText);
                name = name.Replace('"','\'');
                if (name.Contains("\n")) name = name.Substring(0, name.IndexOf('\n') - 1);

                cfg += "\"" + g.GetHashCode().ToString() + "\" [ \n label = \"" +
                    name +
                    "\"\n shape = \"box\"\n];\n";
                foreach (GNode t in g.successors)
                {
                    /*                     
                        "Block0" -> "Block1";
                     */
                    string from =  g.GetHashCode().ToString();
                    string to =  t.GetHashCode().ToString();
                    cfg +=  " \"" + from + "\" -> \"" + to +"\"; \n";
                }
            }
            cfg += "}\n";
            return cfg;
        }

        public SCFGBuilder(BLOCK block)
        {
            allNodes = new LinkedList<GNode>();
            entry = new GNode(null, null, this); //Entry node
            last = entry;
            returns = new LinkedList<GNode>();
            breaks = new LinkedList<GNode>();            
            Visit(block);
            last = new GNode(null, last, this);
            foreach (GNode g in returns) g.successors.AddLast(last);
        }

        protected override void Visit_STATEMENT_LIST(STATEMENT_LIST node)
        {
            for (int i = 0; i < node.Length; i++){
                Visit(node[i]);                
            }
        }
        protected override void Visit_IF_PAIR(IF_PAIR node)
        {
            Visit(node.statements);
        }


        protected override void Visit_ASSIGNMENT(ASSIGNMENT node)
        {
            last = new GNode(node, last, this);

        }
        protected override void Visit_CALL_STMT(CALL_STMT node)
        {
            last = new GNode(node, last, this);
        }
        protected override void Visit_EXIT(EXIT node)
        {
            breaks.AddLast(new GNode(node, last, this));
            last = null;
        }
        protected override void Visit_RETURN(RETURN node)
        {
            returns.AddLast(new GNode(node, last, this));
            last = null;
        }
        protected override void Visit_REPLY(REPLY node)
        {
            last = new GNode(node, last, this);
        }
        protected override void Visit_AWAIT(AWAIT node)
        {
            last = new GNode(node, last, this);
        }
        protected override void Visit_SEND_RECEIVE(SEND_RECEIVE node)
        {
            last = new GNode(node, last, this);
        }
        protected override void Visit_ACCEPT(ACCEPT node)
        {
            last = new GNode(node, last, this);
        }
        protected override void Visit_IF(IF node)
        {
            GNode gnode = new GNode(node, last, this);
            GNode next = new GNode(null, null, this);
            for (int i = 0; i < node.Alternatives.Length; i++)
            {
                last = gnode;
                Visit(node.Alternatives[i]);
                if(last != null) last.successors.AddLast(next);
            }
            last = next;
        }
        protected override void Visit_CASE(CASE node)
        {
            GNode gnode = new GNode(node, last, this);
            GNode next = new GNode(null, null, this);
            for (int i = 0; i < node.Cases.Length; i++)
            {
                last = gnode;
                Visit(node.Cases[i]);
                if(last!=null) last.successors.AddLast(next);
            }
            last = next;
        }
        protected override void Visit_BLOCK(BLOCK node)
        {
            Visit(node.statements);
        }

        protected override void Visit_FOR(FOR node)
        {
            GNode save = new GNode(node, last, this);
            Visit(node.statements);
            if(last!=null) last.successors.AddLast(save);
            last = save;
        }
        protected override void Visit_WHILE(WHILE node)
        {
            GNode save = new GNode(node, last, this);
            Visit(node.statements);
            if (last != null) last.successors.AddLast(save);
            last = save;
        }
        protected override void Visit_REPEAT(REPEAT node)
        {
            GNode save = last;
            Visit(node.statements);
            last = new GNode(node, last, this);
            last.successors.AddLast(save);            
        }
        protected override void Visit_LOOP(LOOP node)
        {
            LinkedList<GNode> savelist = breaks;
            breaks = new LinkedList<GNode>();
            GNode save = last;
            Visit(node.statements);            
            last.successors.AddLast(save);
            last = new GNode(node, null, this);
            foreach (GNode g in breaks) g.successors.AddLast(last);
            breaks = savelist;
        }
    }

}