using AgileConfig.Server.Apisite.Controllers;
using AgileConfig.Server.Apisite.Controllers.api;
using AgileConfig.Server.Apisite.Models;
using AgileConfig.Server.Data.Entity;
using AgileConfig.Server.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ApiSiteTests
{
    [TestClass]
    public class TestAdminController
    {
        [TestMethod]
        public async Task TestLogin()
        {
            var tempData = new Mock<ITempDataDictionary>();
            var settingService = new Mock<ISettingService>();
            var syslogService = new Mock<ISysLogService>();
            var authenticationService = new Mock<IAuthenticationService>();
            authenticationService.Setup(s => s.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(new System.Security.Claims.ClaimsPrincipal(), "")));
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(() => {
                    return authenticationService.Object;
                });

            var ctrl = new AdminController(settingService.Object, syslogService.Object);
            ctrl.ControllerContext = new ControllerContext();
            ctrl.ControllerContext.HttpContext = new DefaultHttpContext();
            ctrl.ControllerContext.HttpContext.RequestServices = sp.Object;
            ctrl.TempData = tempData.Object;

            var act = await ctrl.Login();
            Assert.IsNotNull(act);
            Assert.IsInstanceOfType(act, typeof(RedirectResult));
            var rd = act as RedirectResult;
            Assert.AreEqual("/", rd.Url);

            authenticationService.Setup(s => s.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
               .ReturnsAsync(AuthenticateResult.Fail(""));
            settingService.Setup(s => s.HasAdminPassword())
                .ReturnsAsync(false);

            act = await ctrl.Login();
            Assert.IsNotNull(act);
            Assert.IsInstanceOfType(act, typeof(RedirectResult));
            rd = act as RedirectResult;
            Assert.AreEqual("InitPassword", rd.Url);

            settingService.Setup(s => s.HasAdminPassword())
               .ReturnsAsync(true);

            act = await ctrl.Login();
            Assert.IsNotNull(act);
            Assert.IsInstanceOfType(act, typeof(ViewResult));
        }

        [TestMethod]
        public async Task TestLogin1()
        {
            var tempData = new Mock<ITempDataDictionary>();
            var settingService = new Mock<ISettingService>();
            settingService.Setup(s => s.ValidateAdminPassword("123456")).ReturnsAsync(true);
            var syslogService = new Mock<ISysLogService>();

            var ctrl = new AdminController(settingService.Object, syslogService.Object);
            var act = await ctrl.Login("");
            Assert.IsNotNull(act);
            Assert.IsInstanceOfType(act, typeof(ViewResult));
            var vr = act as ViewResult;
            Assert.IsNotNull(vr);
            Assert.IsNotNull(vr.ViewData);
            var msg = vr.ViewData["ErrorMessage"] as string;
            Assert.IsNotNull(msg);
            Assert.AreEqual("密码不能为空", msg);
            msg = ctrl.ViewBag.ErrorMessage;
            Assert.IsNotNull(msg);
            Assert.AreEqual("密码不能为空", msg);

            act = await ctrl.Login("123");
            Assert.IsNotNull(act);
            Assert.IsInstanceOfType(act, typeof(ViewResult));
            vr = act as ViewResult;
            Assert.IsNotNull(vr);
            Assert.IsNotNull(vr.ViewData);
            msg = vr.ViewData["ErrorMessage"] as string;
            Assert.IsNotNull(msg);
            Assert.AreEqual("登录失败：密码不正确", msg);
            msg = ctrl.ViewBag.ErrorMessage;
            Assert.IsNotNull(msg);
            Assert.AreEqual("登录失败：密码不正确", msg);

            var authenticationService = new Mock<IAuthenticationService>();
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(() => {
                    return authenticationService.Object;
                });
            ctrl.ControllerContext = new ControllerContext();
            ctrl.ControllerContext.HttpContext = new DefaultHttpContext();
            ctrl.ControllerContext.HttpContext.RequestServices = sp.Object;
            act = await ctrl.Login("123456");
            Assert.IsNotNull(act);
            Assert.IsInstanceOfType(act, typeof(RedirectResult));
            var rr = act as RedirectResult;
            Assert.AreEqual("/", rr.Url);
        }


        [TestMethod]
        public async Task TestInitPassword()
        {
            var settingService = new Mock<ISettingService>();
            var syslogService = new Mock<ISysLogService>();

            var ctrl = new AdminController(settingService.Object, syslogService.Object);
            var act = await ctrl.InitPassword("", "");
            Assert.IsNotNull(act);
            var vr = act as ViewResult;
            Assert.IsNotNull(vr);
            var msg = vr.ViewData["ErrorMessage"];
            Assert.AreEqual("密码不能为空", msg);
            act = await ctrl.InitPassword("1", "");
            Assert.IsNotNull(act);
            vr = act as ViewResult;
            Assert.IsNotNull(vr);
            msg = vr.ViewData["ErrorMessage"];
            Assert.AreEqual("密码不能为空", msg);
            act = await ctrl.InitPassword("", "1");
            Assert.IsNotNull(act);
            vr = act as ViewResult;
            Assert.IsNotNull(vr);
            msg = vr.ViewData["ErrorMessage"];
            Assert.AreEqual("密码不能为空", msg);

            act = await ctrl.InitPassword("a122222222222222222222222222222221222222222222222222222222222222" +
                "22a12222222222222222222222222222222122222222222222222222222222222222", "1");
            Assert.IsNotNull(act);
            vr = act as ViewResult;
            Assert.IsNotNull(vr);
            msg = vr.ViewData["ErrorMessage"];
            Assert.AreEqual("密码最长不能超过50位", msg);

            act = await ctrl.InitPassword("1", "2");
            Assert.IsNotNull(act);
            vr = act as ViewResult;
            Assert.IsNotNull(vr);
            msg = vr.ViewData["ErrorMessage"];
            Assert.AreEqual("输入的两次密码不一致", msg);

            settingService.Setup(s => s.HasAdminPassword()).ReturnsAsync(true);
            act = await ctrl.InitPassword("1", "1");
            Assert.IsNotNull(act);
            vr = act as ViewResult;
            Assert.IsNotNull(vr);
            msg = vr.ViewData["ErrorMessage"];
            Assert.AreEqual("密码已经设置过，不需要再次设置", msg);

            settingService.Setup(s => s.HasAdminPassword()).ReturnsAsync(false);
            settingService.Setup(s => s.SetAdminPassword(It.IsAny<string>())).ReturnsAsync(false);
            act = await ctrl.InitPassword("1", "1");
            Assert.IsNotNull(act);
            vr = act as ViewResult;
            Assert.IsNotNull(vr);
            msg = vr.ViewData["ErrorMessage"];
            Assert.AreEqual("初始化密码失败", msg);

            settingService.Setup(s => s.SetAdminPassword(It.IsAny<string>())).ReturnsAsync(true);
            act = await ctrl.InitPassword("1", "1");
            Assert.IsNotNull(act);
            var rr = act as RedirectResult;
            Assert.IsNotNull(rr);
            Assert.AreEqual("InitPasswordSuccess", rr.Url);
        }
    }
}
