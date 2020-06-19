using System;
using System.Collections.Generic;
using ILRuntime.CLR.Method;
using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;
using MessagePack;

public class MemoryDatabaseBaseAdapter : CrossBindingAdaptor
{
    static CrossBindingMethodInfo<Dictionary<System.String, (System.Int32, System.Int32)>, System.Int32, System.Byte[], IFormatterResolver> initMethod = new CrossBindingMethodInfo<Dictionary<System.String, (System.Int32, System.Int32)>, System.Int32, System.Byte[], IFormatterResolver>("Init");
    //static CrossBindingMethodInfo<Dictionary<string, (int, int)>, int, byte[], IFormatterResolver> initMethod = new CrossBindingMethodInfo<Dictionary<string, (int, int)>, int, byte[], IFormatterResolver>("Init");

    public override Type BaseCLRType
    {
        get
        {
            return typeof(MasterMemory.MemoryDatabaseBase);
        }
    }

    public override Type AdaptorType
    {
        get
        {
            return typeof(Adapter);
        }
    }

    public override object CreateCLRInstance(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
    {
        return new Adapter(appdomain, instance);
    }

    public class Adapter : MasterMemory.MemoryDatabaseBase, CrossBindingAdaptorType
    {
        ILTypeInstance instance;
        ILRuntime.Runtime.Enviorment.AppDomain appdomain;

        public Adapter()
        {

        }

        public Adapter(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
        {
            this.appdomain = appdomain;
            this.instance = instance;
        }

        public ILTypeInstance ILInstance { get { return instance; } }

        protected override void Init(Dictionary<System.String, (System.Int32 offset, System.Int32 count)> header, System.Int32 headerOffset, System.Byte[] databaseBinary, IFormatterResolver resolver)
        {
            initMethod.Invoke(this.instance, header, headerOffset, databaseBinary, resolver);
        }
        //protected override void Init(Dictionary<string, (int offset, int count)> header, int headerOffset, byte[] databaseBinary, IFormatterResolver resolver)
        //{
        //    initMethod.Invoke(this.instance, header, headerOffset, databaseBinary, resolver);
        //}

        public override string ToString()
        {
            IMethod m = appdomain.ObjectType.GetMethod("ToString", 0);
            m = instance.Type.GetVirtualMethod(m);
            if (m == null || m is ILMethod)
            {
                return instance.ToString();
            }
            else
                return instance.Type.FullName;
        }
    }
}
