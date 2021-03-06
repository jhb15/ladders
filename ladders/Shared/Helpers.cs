﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ladders.Models;
using ladders.Repositories.Interfaces;
using ladders.Services;
using Newtonsoft.Json;

namespace ladders.Shared
{
    public static class Helpers
    {
        public static bool AmIAdmin(ClaimsPrincipal user)
        {
            return user.HasClaim("user_type", "administrator") || user.HasClaim("user_type", "coordinator");
        }

        public static bool DoIHaveAnAccount(ClaimsPrincipal user, IProfileRepository profileRepository)
        {
            var userId = GetMyName(user);

            return profileRepository.Exists(userId);
        }

        public static string GetMyName(ClaimsPrincipal user)
        {
            return user.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        }

        public static async Task<bool> EmailUser(string commsBaseUrl, IApiClient client, string UserId, string Subject, string Content)
        {
            var result = await client.PostAsync($"{commsBaseUrl}api/Email/ToUser",
                new {Subject, Content, UserId});

            return result.IsSuccessStatusCode;
        }

        public static async Task<bool> ChallengeNotifier(string commsBaseUrl, IApiClient client, string Subject, Challenge challenge)
        {
            var ce = await EmailUser(commsBaseUrl, client, challenge.Challengee.UserId, Subject,
                EmailManager.GetNewChallengeEmail(challenge, false));
            
            var cr = await EmailUser(commsBaseUrl, client, challenge.Challenger.UserId, Subject,
                EmailManager.GetNewChallengeEmail(challenge, true));

            if (!cr || !ce)
            {
                Console.WriteLine($"Error Sending email Challengee:{ce} Challenger:{cr}");
                return false;
            }
            return true;
        }

        public static async Task<IEnumerable<Venue>> GetVenues(string bookingBaseUrl, IApiClient apiClient)
        {
            var venueData = await apiClient.GetAsync($"{bookingBaseUrl}api/venues");

            if (!venueData.IsSuccessStatusCode) return null;

            var info = await venueData.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ICollection<Venue>>(info);
        }

        public static async Task<IEnumerable<Sport>> GetSports(string bookingBaseUrl, IApiClient apiClient)
        {
            var sportData = await apiClient.GetAsync($"{bookingBaseUrl}api/sports");

            if (!sportData.IsSuccessStatusCode) return null;

            var info = await sportData.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ICollection<Sport>>(info);
        }

        public static async Task<bool> FreeUpVenue(string bookingBaseUrl, IApiClient apiClient, int roomId)
        {
            var result =
                await apiClient.DeleteAsync($"{bookingBaseUrl}api/booking/" + roomId);

            return result.IsSuccessStatusCode;
        }

        public static bool IsUserInActiveChallenge(IEnumerable<Challenge> model, ProfileModel user)
        {
            bool Check(Challenge challenge)
            {
                if (challenge.ChallengeeId != user.Id && challenge.ChallengerId != user.Id)
                    return false;

                return !challenge.Resolved;
            }

            return model.Where(Check).Any();
        }

        public static Dictionary<string, bool> GetChallengable(IEnumerable<Challenge> challenges, LadderModel ladder, Ranking rank)
        {
            var users = new Dictionary<string, bool>();
            var allChallenges = challenges.Where(a => a.Ladder == ladder).ToList();
            var usersAbove = ladder.CurrentRankings.Where(a => a.Position < rank.Position).OrderByDescending(a => a.Position).ToList();      
            var added = 0;

            foreach (var user in usersAbove)
            {
                if (user?.User == null || user.User.Suspended)
                    continue;

                if (IsUserInActiveChallenge(allChallenges, user.User)) continue;

                users[user.User.Name] = true;
                added++;

                if (added == 5) return users;
            }

            return users;
        }
    }
}
