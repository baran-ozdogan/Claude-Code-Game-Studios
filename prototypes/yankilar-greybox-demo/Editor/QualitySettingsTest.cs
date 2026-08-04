using System;
using UnityEngine;
using NUnit.Framework;
using System.Reflection;
using UnityEditor.Build;

class QualitySettingsTest 
{
	[Test]
	public void QualityLevelsForEachPlatform() 
    {
	    // Get all named build targets
	    Type type = typeof(NamedBuildTarget);
	    FieldInfo[] fields = type.GetFields();
	    
	    // Iterate over all build targets and check if there are quality levels for each platform
	    for(int i=0; i<fields.Length; i++)
	    {
		    NamedBuildTarget target = (NamedBuildTarget)fields[i].GetValue(null);
		    if(target == NamedBuildTarget.Unknown) continue;
		    string targetName =target.TargetName;
		    int levels = QualitySettings.GetActiveQualityLevelsForPlatformCount(targetName);
		    Assert.Greater(levels, 0, "No quality levels found for " + targetName);
	    }

	}
}
