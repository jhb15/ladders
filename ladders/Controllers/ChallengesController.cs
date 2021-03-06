﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ladders.Models;
using ladders.Repositories.Interfaces;
using ladders.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ladders.Controllers
{
    [Authorize]
    public class ChallengesController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly IConfigurationSection _appConfig;
        private readonly IChallengesRepository _challengesRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly ILaddersRepository _laddersRepository;
        private readonly IBookingRepository _bookingRepository;

        public ChallengesController(IApiClient client, IConfiguration config,
            IChallengesRepository challengesRepository, IProfileRepository profileRepository,
            ILaddersRepository laddersRepository, IBookingRepository bookingRepository)
        {
            _apiClient = client;
            _appConfig = config.GetSection("ladders");
            _challengesRepository = challengesRepository;
            _profileRepository = profileRepository;
            _laddersRepository = laddersRepository;
            _bookingRepository = bookingRepository;
        }

        // GET: Challenges
        public async Task<IActionResult> Index()
        {
            var me = await _profileRepository.GetByUserIdIncAsync(Helpers.GetMyName(User));
            ViewBag.Me = me;
            ViewBag.IsAdmin = Helpers.AmIAdmin(User);

            if (me == null)
                return RedirectToAction("Create", "Profile");

            var challenges = _challengesRepository.GetByChallengee(me);
            challenges.AddRange(_challengesRepository.GetByChallenger(me));

            await ConcedeStaleChallanges(challenges);

            ViewBag.Challenged = _challengesRepository.GetByChallengee(me);
            ViewBag.Challenging = _challengesRepository.GetByChallenger(me);
            return View();
        }

        // GET: Challenges/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var challenge = await _challengesRepository.GetByIdExclUserInfAsync((int) id);
            if (challenge == null || !await IsValid(challenge)) return NotFound();

            var me = await _profileRepository.GetByUserIdIncAsync(Helpers.GetMyName(User));
            ViewBag.BeingChallenged = challenge.Challengee == me;

            return View(challenge);
        }

        // GET: Challenges/Create
        public async Task<IActionResult> Create(int? userId, int? ladderId)
        {
            if (userId == null || ladderId == null) return RedirectToAction(nameof(Index));

            var challengee = await _profileRepository.FindByIdAsync((int) userId);
            var ladder = await _laddersRepository.FindByIdAsync((int) ladderId);
            var me = await _profileRepository.GetByUserIdIncAsync(Helpers.GetMyName(User));

            if (challengee == null || ladder == null || me == null || challengee.Suspended || me.Suspended) return RedirectToAction(nameof(Index));

            if (_challengesRepository.IsUserInActiveChallenge(me) ||
                _challengesRepository.IsUserInActiveChallenge(challengee))
                return RedirectToAction(nameof(Index));

            var challenge = new Challenge
            {
                ChallengedTime = DateTime.UtcNow,
                Created = DateTime.UtcNow,
                Resolved = false,
                Challenger = me,
                Challengee = challengee,
                Ladder = ladder,
                Result = Winner.Unresolved
            };

            var facilities = await Helpers.GetVenues(_appConfig.GetValue<string>("BookingFacilitiesUrl"), _apiClient);
            var sports = await Helpers.GetSports(_appConfig.GetValue<string>("BookingFacilitiesUrl"), _apiClient);

            if (sports == null || facilities == null)
                return RedirectToAction(nameof(Index));

            ViewBag.VenueId = new SelectList(facilities, "venueId", "venueName");
            ViewBag.SportId = new SelectList(sports, "sportId", "sportName");

            return View(challenge);
        }

        // POST: Challenges/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ChallengedTime,Challenger,Challengee,Ladder,Result")]
            Challenge challenge, [Bind("VenueId")] int venueId, [Bind("SportId")] int sportId)
        {
            //if (!ModelState.IsValid) return View(challenge);

            challenge.ChallengeeId = challenge.Challengee.Id;
            challenge.ChallengerId = challenge.Challenger.Id;

            var user = await _profileRepository.GetByUserIdIncAsync(Helpers.GetMyName(User));

            if (_challengesRepository.IsUserInActiveChallenge(user) ||
                _challengesRepository.IsUserInActiveChallenge(challenge.Challengee))
                return NotFound();

            challenge.Challenger = null;
            challenge.Challengee = null;

            if (challenge.Ladder == null)
                return View(challenge);

            var booking = await MakeBooking(venueId, sportId, challenge.ChallengedTime);

            if (booking == null)
                return View(challenge);

            challenge.Created = DateTime.UtcNow;
            await _bookingRepository.AddAsync(booking);
            challenge.Booking = booking;

            await _challengesRepository.AddAsync(challenge);

            challenge = await _challengesRepository.GetFullChallenge(challenge.Id);

            await Helpers.ChallengeNotifier(_appConfig.GetValue<string>("CommsUrl"), _apiClient, "Challenge Creation Notification", challenge);
            return RedirectToAction(nameof(Details), new {challenge.Id});
        }

        // GET: Challenges/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var challenge = await _challengesRepository.GetByIdIncDirectDecentAsync((int) id);

            if (challenge == null || !await IsValid(challenge)) return NotFound();

            var facilities = await Helpers.GetVenues(_appConfig.GetValue<string>("BookingFacilitiesUrl"), _apiClient);
            var sports = await Helpers.GetSports(_appConfig.GetValue<string>("BookingFacilitiesUrl"), _apiClient);

            if (sports == null || facilities == null)
                return RedirectToAction(nameof(Index));

            ViewBag.VenueId = new SelectList(facilities, "venueId", "venueName");
            ViewBag.SportId = new SelectList(sports, "sportId", "sportName");

            return View(challenge);
        }

        // POST: Challenges/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] DateTime ChallengedTime, [Bind("VenueId")] int venueId, [Bind("SportId")] int sportId)
        {
            var challenge = await _challengesRepository.GetByIdIncDirectDecentAsync((int)id);
            if (challenge == null)
                return NotFound();

            try
            {
                challenge.ChallengedTime = ChallengedTime;
                challenge.ChallengeeId = challenge.Challenger.Id;
                challenge.ChallengerId = challenge.Challengee.Id;

                challenge.Challenger = null;
                challenge.Challengee = null;

                await Helpers.FreeUpVenue(_appConfig.GetValue<string>("BookingFacilitiesUrl"), _apiClient, challenge.Booking.bookingId);

                var booking = await MakeBooking(venueId, sportId, challenge.ChallengedTime);

                if (booking == null)
                    return View(challenge);

                challenge.Created = DateTime.UtcNow;
                await _bookingRepository.AddAsync(booking);
                challenge.Booking = booking;
                await _challengesRepository.UpdateAsync(challenge);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChallengeExists(challenge.Id)) return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Challenges/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var challenge = await _challengesRepository.FindByIdAsync((int) id);
            if (challenge == null) return NotFound();

            return View(challenge);
        }

        // POST: Challenges/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var challenge = await _challengesRepository.FindByIdAsync(id);
            await _challengesRepository.DeleteAsync(challenge);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> AcceptChallenge(int id)
        {
            var challenge = await _challengesRepository.FindByIdAsync(id);

            if (challenge == null) return NotFound();

            var me = await _profileRepository.GetByUserIdIncAsync(Helpers.GetMyName(User));

            if (me == null || challenge.Challengee != me) return NotFound();

            challenge.Accepted = true;
            await _challengesRepository.UpdateAsync(challenge);

            return RedirectToAction(nameof(Details), new {id});
        }

        [HttpGet]
        public async Task<IActionResult> Concede(int id)
        {
            var challenge = await _challengesRepository.FindByIdAsync(id);

            if (challenge == null) return NotFound();

            var me = await _profileRepository.GetByUserIdIncAsync(Helpers.GetMyName(User));

            if (me == null || challenge.Challengee != me || challenge.Accepted || challenge.Resolved) return NotFound();

            return View(challenge);
        }

        [HttpPost]
        public async Task<IActionResult> ConcedeConfirm(int id)
        {
            var challenge = await _challengesRepository.GetFullChallenge(id);

            if (challenge == null)
                return NotFound();

            var me = await _profileRepository.GetByUserIdIncAsync(Helpers.GetMyName(User));

            if (me == null || challenge.Challengee != me)
                return NotFound();

            await Helpers.FreeUpVenue(_appConfig.GetValue<string>("BookingFacilitiesUrl"), _apiClient, challenge.Booking.bookingId);

            challenge = await _challengesRepository.UserConcedeChallenge(me, _apiClient,
                _appConfig.GetValue<string>("BookingFacilitiesUrl"), challenge);
            await _laddersRepository.UpdateLadder(challenge);

            return RedirectToAction(nameof(Details), new {id});
        }

        [HttpPost]
        public async Task<IActionResult> UserLost(int id)
        {
            var challenge = await _challengesRepository.GetFullChallenge(id);

            if (challenge == null)
                return NotFound();

            var me = await _profileRepository.GetByUserIdIncAsync(Helpers.GetMyName(User));

            if (me == null || challenge.Challengee != me && challenge.Challenger != me)
                return NotFound();

            challenge = await _challengesRepository.UserConcedeChallenge(me, _apiClient,
                _appConfig.GetValue<string>("BookingFacilitiesUrl"), challenge);
            await _laddersRepository.UpdateLadder(challenge);

            return RedirectToAction(nameof(Details), new {id});
        }

        private bool ChallengeExists(int id)
        {
            return _challengesRepository.Exists(id);
        }

        public async Task<Booking> MakeBooking(int venueId, int sportId, DateTime time)
        {
            var res = await _apiClient.PostAsync(
                $"{_appConfig.GetValue<string>("BookingFacilitiesUrl")}api/booking/{venueId}/{sportId}",
                new {bookingDateTime = time, userId = Helpers.GetMyName(User)});
            if (!res.IsSuccessStatusCode)
                return null;

            var data = await res.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Booking>(data);
        }

        public async Task<bool> IsValid(Challenge challenge)
        {
            var me = await _profileRepository.GetByUserIdIncAsync(Helpers.GetMyName(User));
            var isAdmin = Helpers.AmIAdmin(User);
            return challenge.Challenger == me || challenge.Challengee == me || !isAdmin;
        }

        public async Task ConcedeStaleChallanges(IEnumerable<Challenge> challenges)
        {
            foreach (var challenge in challenges)
            {
                if (!_challengesRepository.IsChallengeStale(challenge)) 
                    continue;

                var chal = await _challengesRepository.UpdateWinner(Winner.Challenger, _apiClient,
                    _appConfig.GetValue<string>("CommsUrl"), challenge);

                await _laddersRepository.UpdateLadder(chal);
            }
        }
    }
}