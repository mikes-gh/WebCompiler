﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using WebCompiler;

namespace WebCompilerTest
{
    //[TestClass]
    public class CoffeeScriptTest
    {
        private ConfigFileProcessor _processor;

        [TestInitialize]
        public void Setup()
        {
            _processor = new ConfigFileProcessor();
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete("../../../artifacts/coffee/test.js");
            File.Delete("../../../artifacts/coffee/test.min.js");
            File.Delete("../../../artifacts/coffee/test.js.map");
        }

        [TestMethod, TestCategory("CoffeeScript")]
        public void CompileCoffeeScript()
        {
            _ = _processor.Process("../../../artifacts/coffeeconfig.json");
            var js = new FileInfo("../../../artifacts/coffee/test.js");
            var min = new FileInfo("../../../artifacts/coffee/test.min.js");
            var map = new FileInfo("../../../artifacts/coffee/test.js.map");
            Assert.IsTrue(js.Exists, "Output file doesn't exist");
            Assert.IsFalse(min.Exists, "Min file exists");
            Assert.IsTrue(map.Exists, "Map file doesn't exist");
            Assert.IsTrue(js.Length > 5);
            Assert.IsTrue(map.Length > 5);
        }

        [TestMethod, TestCategory("CoffeeScript")]
        public void CompileCoffeeScriptWithError()
        {
            var result = _processor.Process("../../../artifacts/coffeeconfigerror.json");
            var error = result.First().Errors[0];
            Assert.AreEqual(1, error.LineNumber);
            Assert.AreEqual("unexpected ==", error.Message);
        }
    }
}