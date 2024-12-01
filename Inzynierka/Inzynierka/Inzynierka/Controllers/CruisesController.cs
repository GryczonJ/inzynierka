﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Inzynierka.Data;
using Inzynierka.Data.Tables;
using System.Security.Claims;

namespace Inzynierka.Controllers
{
    public class CruisesController : Controller
    {
        private readonly AhoyDbContext _context;

        public CruisesController(AhoyDbContext context)
        {
            _context = context;
        }

        // GET: Cruises
        /*public async Task<IActionResult> Index()
        {
            var ahoyDbContext = _context.Cruises.Include(c => c.Capitan).Include(c => c.Yacht);
            return View(await ahoyDbContext.ToListAsync());
        }*/
        public async Task<IActionResult> Index()
        {
            // Pobranie identyfikatora zalogowanego użytkownika
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdString == null || !Guid.TryParse(userIdString, out Guid userId))
            {
                return RedirectToAction("Login", "Account"); // Przekierowanie do logowania, jeśli użytkownik nie jest zalogowany
            }

            // Rejsy użytkownika (gdzie użytkownik jest kapitanem)
            var myCruises = await _context.Cruises
                .Include(c => c.Capitan)
                .Include(c => c.Yacht)
                .Where(c => c.CapitanId == userId)
                .ToListAsync();

            // Rejsy pozostałe (nie prowadzone przez użytkownika)
            var otherCruises = await _context.Cruises
                .Include(c => c.Capitan)
                .Include(c => c.Yacht)
                .Where(c => c.CapitanId != userId)
                .ToListAsync();

            // Przekazanie obu list do widoku jako krotki
            return View((myCruises.AsEnumerable(), otherCruises.AsEnumerable()));
        }


        [HttpPost]
        public async Task<IActionResult> JoinCruise(int cruiseId)
        {
            // Pobranie identyfikatora zalogowanego użytkownika
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdString == null || !Guid.TryParse(userIdString, out Guid userId))
            {
                return RedirectToAction("Login", "Account"); // Przekierowanie do logowania, jeśli użytkownik nie jest zalogowany
            }

            // Sprawdzenie, czy istnieje już zgłoszenie
            var existingRequest = await _context.CruiseJoinRequest
                .FirstOrDefaultAsync(r => r.CruiseId == cruiseId && r.UserId == userId);

            if (existingRequest != null)
            {
                TempData["Error"] = "Już złożyłeś prośbę o dołączenie do tego rejsu.";
                //return RedirectToAction(nameof(Details));
                return RedirectToAction(nameof(Details), new { id = cruiseId });

            }

            // Pobranie danych o kapitanie
            var cruise = await _context.Cruises
                .Include(c => c.Capitan)
                .FirstOrDefaultAsync(c => c.Id == cruiseId);

            if (cruise == null)
            {
                TempData["Error"] = "Rejs nie istnieje.";
                //return RedirectToAction(nameof(Details));
                return RedirectToAction(nameof(Details), new { id = cruiseId });

            }

            // Utworzenie zgłoszenia
            var joinRequest = new CruiseJoinRequest
            {
                CruiseId = cruiseId,
                UserId = userId,
                CapitanId = cruise.Capitan.Id,
                status = "Pending",
                date = DateTime.UtcNow
            };

            _context.CruiseJoinRequest.Add(joinRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Prośba o dołączenie została wysłana.";
            //return RedirectToAction(nameof(Details));
            return RedirectToAction(nameof(Details), new { id = cruiseId });
        }

        // GET: Cruises/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cruises = await _context.Cruises
                .Include(c => c.Capitan)
                .Include(c => c.Yacht)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cruises == null)
            {
                return NotFound();
            }

            return View(cruises);
        }

        // GET: Cruises/Create
        public IActionResult Create()
        {
            /*ViewData["CapitanId"] = new SelectList(_context.Users, "Id", "Id");
            ViewData["YachtId"] = new SelectList(_context.Yachts, "Id", "name");
            return View();*/

            // Pobranie zalogowanego użytkownika jako string
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Sprawdzenie, czy wartość istnieje i jest poprawnym Guidem
            if (userIdString != null && Guid.TryParse(userIdString, out Guid ownerId))
            {
                // Filtrowanie jachtów tylko dla zalogowanego użytkownika
                var userYachts = _context.Yachts.Where(y => y.OwnerId == ownerId).ToList();

                // Przekazanie tylko jachtów zalogowanego użytkownika do widoku
                ViewData["YachtId"] = new SelectList(userYachts, "Id", "name");
            }
            else
            {
                // Jeśli użytkownik nie jest zalogowany, lista jachtów będzie pusta
                ViewData["YachtId"] = new SelectList(Enumerable.Empty<object>(), "Id", "name");
                ModelState.AddModelError(string.Empty, "Nie jesteś zalogowany. Proszę zalogować się.");
            }

            return View();
        }

        // POST: Cruises/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,name,description,destination,start_date,end_date,price,currency,status,maxParticipants,YachtId,CapitanId")] Cruises cruises)
        {
            // Pobranie zalogowanego użytkownika jako string
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Sprawdzenie, czy wartość istnieje
            if (userIdString != null && Guid.TryParse(userIdString, out Guid ownerId))
            {
                cruises.CapitanId = ownerId;
            }
            else
            {
                // Obsługa błędu: brak lub nieprawidłowy identyfikator użytkownika
                //ModelState.AddModelError(string.Empty, "Nie można przypisać użytkownika jako właściciela.");
                // Obsługa błędu: brak lub nieprawidłowy identyfikator użytkownika
                ModelState.AddModelError(string.Empty, "Nie jesteś zalogowany jako właściciel. Proszę zalogować się.");
                return View(cruises);
            }

            ModelState.Clear();
            if (ModelState.IsValid)
            {
                _context.Add(cruises);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CapitanId"] = new SelectList(_context.Users, "Id", "Id", cruises.CapitanId);
            ViewData["YachtId"] = new SelectList(_context.Yachts, "Id", "name", cruises.YachtId);
            return View(cruises);
        }

        // GET: Cruises/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cruises = await _context.Cruises.FindAsync(id);
            if (cruises == null)
            {
                return NotFound();
            }
            // Pobranie zalogowanego użytkownika jako string
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Sprawdzenie, czy wartość istnieje i jest poprawnym Guidem
            if (userIdString != null && Guid.TryParse(userIdString, out Guid ownerId))
            {
                // Filtrowanie jachtów tylko dla zalogowanego użytkownika
                var userYachts = _context.Yachts.Where(y => y.OwnerId == ownerId).ToList();

                // Przekazanie tylko jachtów zalogowanego użytkownika do widoku
                ViewData["YachtId"] = new SelectList(userYachts, "Id", "name");
            }
            else
            {
                // Jeśli użytkownik nie jest zalogowany, lista jachtów będzie pusta
                ViewData["YachtId"] = new SelectList(Enumerable.Empty<object>(), "Id", "name");
                ModelState.AddModelError(string.Empty, "Nie jesteś zalogowany. Proszę zalogować się.");
            }

            //ViewData["CapitanId"] = new SelectList(_context.Users, "Id", "Id", cruises.CapitanId);
            ViewData["YachtId"] = new SelectList(_context.Yachts, "Id", "name", cruises.YachtId);
            return View(cruises);
        }

        // POST: Cruises/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,name,description,destination,start_date,end_date,price,currency,status,maxParticipants,YachtId,CapitanId")] Cruises cruises)
        {
            if (id != cruises.Id)
            {
                return NotFound();
            }

            // Pobranie zalogowanego użytkownika jako string
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Sprawdzenie, czy wartość istnieje
            if (userIdString != null && Guid.TryParse(userIdString, out Guid ownerId))
            {
                cruises.CapitanId = ownerId;
            }
            else
            {
                // Obsługa błędu: brak lub nieprawidłowy identyfikator użytkownika
                //ModelState.AddModelError(string.Empty, "Nie można przypisać użytkownika jako właściciela.");
                // Obsługa błędu: brak lub nieprawidłowy identyfikator użytkownika
                ModelState.AddModelError(string.Empty, "Nie jesteś zalogowany jako właściciel. Proszę zalogować się.");
                return View(cruises);
            }

            ModelState.Clear();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cruises);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CruisesExists(cruises.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CapitanId"] = new SelectList(_context.Users, "Id", "Id", cruises.CapitanId);
            ViewData["YachtId"] = new SelectList(_context.Yachts, "Id", "name", cruises.YachtId);
            return View(cruises);
        }

        // GET: Cruises/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cruises = await _context.Cruises
                .Include(c => c.Capitan)
                .Include(c => c.Yacht)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cruises == null)
            {
                return NotFound();
            }

            return View(cruises);
        }

        // POST: Cruises/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cruises = await _context.Cruises.FindAsync(id);
            if (cruises != null)
            {
                _context.Cruises.Remove(cruises);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CruisesExists(int id)
        {
            return _context.Cruises.Any(e => e.Id == id);
        }
    }
}
