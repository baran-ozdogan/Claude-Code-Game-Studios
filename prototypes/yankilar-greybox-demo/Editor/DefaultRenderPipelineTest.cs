using NUnit.Framework;
using UnityEngine.Rendering;

class DefaultRenderPipelineTest
{
	[Test]
	public void CheckDefaultRenderPipeline() 
    {
	    Assert.NotNull(GraphicsSettings.defaultRenderPipeline, "Default Render Pipeline in Project Settings > Graphics is null.");
	}
}
