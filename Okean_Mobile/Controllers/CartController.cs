using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okean_Mobile.Data;
using Okean_Mobile.Models;
using System.Security.Claims;

namespace Okean_Mobile.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        
    }
}