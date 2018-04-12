//by chkrr00k GNU GPLv3 license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text.RegularExpressions;

namespace ConsoleApplication60 {

    abstract class Symbol {
        //Describes all objects that can be parsed and evaluated;

        //Factory pattern;
        public static Symbol of(string input) {
            double numberVal;

            input = input.Trim();
            if(Regex.IsMatch(input, Variable.VARIABLE)) {
                return new Variable(input);
            } else if(input.IndexOfAny(new[] { '+', '-', '*', '/' }) > -1) {
                return new Operand(input);
            } else if(double.TryParse(input, out numberVal)) {
                return new Number(numberVal);
            } else if(Regex.IsMatch(input, ArrayValue.ARRAY)) {
                return new ArrayValue(input);
            } else {
                throw new ArgumentException("Invalid symbol " + input);
            }
        }
    }

    class Operand: Symbol {
        private string type;
        //Describes an operator such as +, -, * and /;
        public Operand(string type) {
            if(type.Trim().IndexOfAny(new[] { '+', '-', '*', '/' }) < 0) {
                throw new ArgumentException("You can only use + - * / as type. Error in " + type);
            }
            this.type = type.Trim();
        }

        //returns the operator itself for the dt parser;
        public override string ToString() {
            return this.type;
        }
    }

    abstract class IndirectValue: Symbol {
        public abstract void setValue(string value);
        public abstract string getName();
    }

    class Variable: IndirectValue {
        private string varName;
        private string varValue;
        public const string VARIABLE = "^\\$[A-Z]+$";

        //Descrive a textual string in the pattern of $NAME that have a numerical value in the environment;
        public Variable(string name) {
            if(!Regex.IsMatch(name.Trim(), Variable.VARIABLE)) {
                throw new ArgumentException("Variable name can only starts witn $, must not contains numbers and must be UPPERCASE. Error in " + varName);
            } else {
                this.varName = name.Trim();
                this.varValue = "0";
            }
        }

        //returns the value of the variable for the dt parser. If it wasn't setted by the caller default is "0" (string zero);
        public override string ToString() {
            return this.varValue;
        }

        public override void setValue(string value) {
            this.varValue = value;
        }

        public override string getName() {
            return this.varName;
        }
    }

    class ArrayValue: IndirectValue {
        private string varName;
        private string varValue;
        public const string ARRAY = "^([A-Z]+)\\[(\\d+)\\]$";
        public const string ARRAYNAME = "^\\@[A-Z]+$";

        //Descrive an array of values (string) in the pattern of @NAME=[ <values> ];
        public ArrayValue(string name) {
            if(!Regex.IsMatch(name.Trim(), ArrayValue.ARRAY)) {
                throw new ArgumentException("Array indexing must be in form of ARRAY[ <index> ]. Error in " + varName);
            } else {
                this.varName = name.Trim();
                this.varValue = "0";
            }
        }

        //returns the value of the variable for the dt parser. If it wasn't setted by the caller default is "0" (string zero);
        public override string ToString() {
            return this.varValue;
        }

        public override void setValue(string value) {
            this.varValue = value;
        }

        public override string getName() {
            return this.varName;
        }
    }

    class Number: Symbol {
        private double value;

        //Describe a real number in the standard C# format.
        public Number(double number) {
            this.value = number;
        }

        //returns the number itself for the dt parser in string form;
        public override string ToString() {
            return this.value.ToString();
        }
    }

    class Assignment: Symbol {
        //Descrive an assignment of a variable in the pattern of $NAME=( <value> );
        public const string VARASSIGNMENT = "^(\\$[A-Z]+)=\\( (.+) \\)$";
        public const string ARRAYASSIGNMENT = "^(\\@[A-Z]+)=\\[ ((\\d|,)+) \\]$";
    }

    class Environment {
        private Dictionary<string, Environment.Object> variables;

        //Describes all possible settings / variables value that the user can set;
        public Environment() {
            this.variables = new Dictionary<string, Environment.Object>();
        }

        //Add a variable;
        public void registerVariable(string name, string value) {
            if(Regex.IsMatch(name, Variable.VARIABLE)) {
                this.variables.Add(name, new Environment.VariableObj(name, value));
            } else if(Regex.IsMatch(name, ArrayValue.ARRAYNAME)) {
                this.variables.Add(name, new Environment.ArrayObj(name, value.Split(',')));
            } else {
                throw new ArgumentException("Variable name can only starts witn $, must not contains numbers and must be UPPERCASE. Error in " + name);
            }

        }
        //Remove a variable;
        public void unregisterVariable(string name, string value) {
            this.variables.Remove(name);
        }
        //Get the variable value;
        public string getVariableValue(string name) {
            if(Regex.IsMatch(name, Variable.VARIABLE)) {
                if(this.variables.ContainsKey(name)) {
                    if(this.variables[name] is Environment.VariableObj) {
                        return ((Environment.VariableObj)this.variables[name]).Value;
                    } else {
                        throw new InvalidCastException("Internal error, but maybe you did something wrong too... Check " + name);
                    }
                } else {
                    throw new KeyNotFoundException("The searched variable wasn't defined");
                }
            }else if(Regex.IsMatch(name, ArrayValue.ARRAY)) {
                Match m = Regex.Match(name, ArrayValue.ARRAY);
                if(m.Success) {
                    if(this.variables.ContainsKey("@" + m.Groups[1].Value)) {
                        if(this.variables["@" + m.Groups[1].Value] is Environment.ArrayObj) {
                            try {
                                return ((Environment.ArrayObj)this.variables["@" + m.Groups[1].Value])[int.Parse(m.Groups[2].Value)];
                            }catch(IndexOutOfRangeException e) {
                                throw e;
                            }
                        } else {
                            throw new InvalidCastException("Internal error, but maybe you did something wrong too... Check " + name);
                        }
                    } else {
                        throw new KeyNotFoundException("Array wasn't defined. Error in: " + name);
                    }
                }else {
                    throw new KeyNotFoundException("Error in " + name + ". Array doesn't Exist");
                }
            }else {
                throw new NotImplementedException("Invalid syntax in " + name);
            }
        }

        private abstract class Object {

        }

        private class ArrayObj: Environment.Object {
            private string[] values;
            private string name;
            public ArrayObj(string name, string[] values) {
                this.values = values;
                this.name = name;
            }
            public string this[int i]{
                get { if(i > this.values.Length || i < 0) {
                        throw new IndexOutOfRangeException("Index of " + i + " in " + this.name + " array does not exist");
                    } else {
                        return this.values[i];
                    } }
                set { this.values[i] = value; }
            }
            public string Name { get { return this.name; } }
            public string Values { get { return string.Join(",", this.values); } }
        }

        private class VariableObj: Environment.Object {
            private string value;
            private string name;
            public VariableObj(string name, string value) {
                this.value = value;
                this.name = name;
            }
            public string Name { get { return this.name; } }
            public string Value { get { return this.value; } }
        }

    }

    class Line {
        private List<Symbol> symbols;
        private bool resolved;
        public enum Type { NONE, VAR, ARRAY }
        private Type assignmentType;
        public Type AssignmentType { get { return this.assignmentType; } }
        private string assignmentVar;
        public string AssignmentVar { get { return this.assignmentVar; } }
        // for arrays;
        private string value;

        //Decribes a line that need evaluation;
        public Line(string input) {
            Match m = null;

            this.symbols = new List<Symbol>();
            this.resolved = false;
            this.assignmentType = Line.Type.NONE;
            input = input.Trim();
            if(Regex.IsMatch(input, Assignment.VARASSIGNMENT)) {
                m = Regex.Match(input, Assignment.VARASSIGNMENT);
                if(m.Success) {
                    this.assignmentVar = m.Groups[1].Value;
                    input = m.Groups[2].Value;
                    this.assignmentType = Line.Type.VAR;
                }
            }else if(Regex.IsMatch(input, Assignment.ARRAYASSIGNMENT)) {
                this.assignmentType = Line.Type.ARRAY;
            }
            switch(this.AssignmentType) {
                case Line.Type.NONE:
                case Line.Type.VAR:
                    // 1) tokenize string with space as separator;
                    // 2) convert into list;
                    // 3) call the Any function to check if at least one is an empty string. (two spaces when one space is a separator generates a null string);
                    // If that happens an Exception is thrown;
                    if(input.Split(' ').ToList().Any(s => string.IsNullOrEmpty(s))) {
                        throw new ArgumentException("Only one SPACE as separator");
                    }
                    // 1) tokenize string with space as separator;
                    // 2) convert into list;
                    // 3) for each add them into the initialized list (with the factory);
                    input.Trim().Split(' ').ToList().ForEach(s => this.symbols.Add(Symbol.of(s)));
                    break;
                case Line.Type.ARRAY:
                    m = Regex.Match(input, Assignment.ARRAYASSIGNMENT);
                    if(m.Success) {
                        this.assignmentVar = m.Groups[1].Value;
                        this.value = m.Groups[2].Value;
                    }
                    break;
                default:
                    throw new ArgumentException("Illegal Operation in " + input);
            }
        }

        //Resolve all eventual variable name;
        public void resolve(Environment env) {
            if(!this.resolved) {
                // 1) filter all symbols to find all Variable;
                // 2) casts them as Variable because they were Symbols;
                // 3) convert it as a List to call the ForEach;
                // 4) for each Variable set its value as the environment say its the proper value
                // /!\ IF VARABLE IS MISSING EXCEPTION IS THROWN HERE   
                this.symbols.FindAll(s => s is IndirectValue).Cast<IndirectValue>().ToList().ForEach(v => v.setValue(env.getVariableValue(v.getName())));
                this.resolved = true;
            }
        }

        //Return the line to evaluate for the evaulator;
        public string getEvalLine() {
            switch(this.AssignmentType) {
                case Line.Type.NONE:
                case Line.Type.VAR:
                    if(!this.resolved) {
                        throw new ArgumentException("Eventual variable names MUST be resolved");
                    } else {
                        // use the overridden ToString();
                        return string.Join("", this.symbols);
                    }
                    case Line.Type.ARRAY:
                    return this.value;
                default:
                    return "";
            }
        }

        //To avoid to call two functions instead of one;
        public string getEvalLine(Environment env) {
            this.resolve(env);
            return this.getEvalLine();
        }

        public override string ToString() {
            return string.Join("", this.symbols);
        }
    }

    class Evaluator {
        private Environment env;
        private DataTable dt;

        //The evaluator itself!;
        public Evaluator(Environment env) {
            this.env = env;
            //Mystery magical component;
            dt = new DataTable();
        }
        public Evaluator() : this(new Environment()) { }

        public Environment environment { get { return this.env; } set { this.env = value; } }

        public string evaluate(Line line) {
            string result;
            switch(line.AssignmentType) {
                case Line.Type.NONE:
                case Line.Type.VAR:
                    try {
                        //Calls the Computed method on the getEvalLine of the passed line. NOTE THAT EVENTUAL VARIABLES ARE RESOLVED!;
                        result = this.dt.Compute(line.getEvalLine(this.env), "").ToString();
                        if(line.AssignmentType == Line.Type.VAR) {
                            this.env.registerVariable(line.AssignmentVar, result);
                        }
                        return result;
                    } catch(KeyNotFoundException e) {
                        throw e;
                    }
                case Line.Type.ARRAY:
                    this.env.registerVariable(line.AssignmentVar, line.getEvalLine());
                    return "NOP";
                default:
                    return "NOP";
            }

        }
    }

    // SYNTAX:
    // 	accepted symbols:
    // 		> numbers (0-9)
    //	 	> variables name in format of $NAME (ie only uppercase, literal, and start with $ string)
    // 		> operators such as +, -, *, and /
    //      > array declaration in the format of @ARRAY=[ <elements> ] where elements are numbers separated by commas and no space (ie @ARRAY=[ 1,2 ])
    //      > array index extraction in format of ARRAY[<index>] where <index> is a decimal integer number
    // 		every symbols must be separated by only ONE space
    // 	to assign a variable syntax is:
    // 		$VARNAME=( <expression> )
    // 	nested variables assignment are forbidden
    // 	parentheses are not accepted as precedence operator, to simulate that define a temp variable and process that variable in the next line. eg to make 2 / ( 2 + 9 ):
    // 		$PRECEDENCE=( 2 + 9 )
    // 		2 / $PRECEDENCE
    // CALLER OPTIONS:
    // 	> create an Environment object
    // 	> if wanted it''s possible to use the registerVariable(), unregisterVariable() and getVariableValue() to get and set entual variables name and value. Those variables name must have the correct syntax as specified above.
    // 	> create an Evaluator object. Constructor wants an Environment to resolve variables names and other functionality. Use the created Environment object. If no Environment are setted an empty one will be generated by the evaluator itself.
    // 	> the evaluate() function in the Evaluator context provides the evaluation function itself. To use that it's needed to create a Line object which rapresent a line of scripting code. That line needs to comply with the syntax above or Exceptions are thrown. That function returns a string rapresenting the result. Eventual assigned variables are setted in the provided environment. NOTE that eventual environment changes override previous variable setings.
    // UML:
    // 	   <<abstract>>
    // 	      Syntax  <----------------<<use>> --------- Line <-------<<use>>------- Evaluator --------<<use>>-------> Environment
    //	    + _of()_                                 + AssignmentType                + environment                 + getVariableValue()
    //	          |                                  + AssignmentVar                 + evaluate()                  + registerVariable()
    //      ______|___________________________                                                                     + unregisterVariable()
    //     |         |	         |            |                                                                       
    //  Number    Operand  IndirectValue  Assignment
    //                           |
    //                    _______|_________
    //                   |                 |
    //                Variable         ArrayValue
    //
    //
    // INSIDE Environment nested class are defined (as private)
    //    <<abstract>>
    //       Object
    //     ____|______
    //    |           |
    // ArrayObj   VariableObj
    //
    //
    //
    class Program {
        static void Main(string[] args) {
            Environment e = new Environment();
            Evaluator eval = new Evaluator(e);
            eval.evaluate(new Line("$ASS=( 2 + 10 )"));
            Console.WriteLine(e.getVariableValue("$ASS"));
            e.registerVariable("$TWO", "2");
            Console.WriteLine(eval.evaluate(new Line("$ASS / $TWO")));

            Console.WriteLine(eval.evaluate(new Line("@ARRAY=[ 2,3 ]")));
            Console.WriteLine(e.getVariableValue("ARRAY[0]"));
            Console.WriteLine(eval.evaluate(new Line("ARRAY[0] + 2")));

            Console.ReadKey();
        }
    }
}
