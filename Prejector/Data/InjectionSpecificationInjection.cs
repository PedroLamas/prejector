﻿using System;
using System.Diagnostics;
using System.Linq;
using PreJector;

[DebuggerDisplay("{ConcreteClassName}")]
public partial class InjectionSpecificationInjection
{
    public bool ShouldHaveConcreteCoreRender
    {
        get { return Interface != null && Interface.Length > 1; }
    }

    public string ConcreteRenderName
    {
        get { return ShouldHaveConcreteCoreRender ? "Concrete" : "Kernel"; }
    }

    public string ConcreteClassName
    {
        get
        {
            string name;

            if (Interface != null && Interface.Length == 1)
            {
                name = Interface[0].InterfaceNameNoI;
            }
            else if (string.IsNullOrEmpty(Concrete))
            {
                if (string.IsNullOrEmpty(Provider))
                {
                    string message =
                        String.Format("ConcreteYieldingFunction - Does not define the required information");
                    throw new InvalidOperationException(message);
                }

                name = Provider;
            }
            else
            {
                name = Concrete;
            }

            return string.Format("{0}_{1}", ConcreteRenderName, name).RemoveDodgyTokens();
        }
    }

    public string InterfaceYieldingFunction(string interfaceRequired, bool isLazy)
    {
        var @interface = Interface.SingleOrDefault(x => x.Value == interfaceRequired);

        if (@interface == null)
        {
            throw new Exception("Why does it not exist?");
        }

        if (isLazy)
        {
            return "Kernel_" + @interface.InterfaceNameNoI + ".GetLazy()";
        }

        return "Kernel_" + @interface.InterfaceNameNoI + ".Get()";
    }

    public string ConcreteYieldingFunction(bool isLazy)
    {
        if (isLazy)
        {
            return ConcreteClassName + ".GetLazy()";
        }

        return ConcreteClassName + ".Get()";
    }

    public bool IsViewModel
    {
        get { return !String.IsNullOrEmpty(Concrete) && Concrete.EndsWith("ViewModel"); }
    }
}