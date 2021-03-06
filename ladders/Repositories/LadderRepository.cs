using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ladders.Models;
using ladders.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ladders.Repositories
{
    public class LadderRepository : ILaddersRepository
    {
        private readonly LaddersContext _context;

        public LadderRepository(LaddersContext context)
        {
            _context = context;
        }
        
        public async Task<LadderModel> FindByIdAsync(int id)
        {
            return await _context.LadderModel.FindAsync(id);
        }

        public async Task<LadderModel> GetByIdAsync(int id)
        {
            return await _context.LadderModel
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<LadderModel> GetByIdIncAllAsync(int id)
        {
            return  await _context.LadderModel
                .Include(m => m.ApprovalUsersList)
                .Include(o => o.CurrentRankings)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<LadderModel> GetByIdIncAllAndUserRankAsync(int id)
        {
            return  await _context.LadderModel
                .Include(ladder => ladder.ApprovalUsersList)
                .Include(ladder => ladder.CurrentRankings)
                .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<LadderModel> GetAllForDeleteAsync(int id)
        {
            return await _context.LadderModel
                .Include(ladder => ladder.ApprovalUsersList)
                .Include(ladder => ladder.CurrentRankings)
                .ThenInclude(a => a.User)
                .Include(ladder => ladder.CurrentRankings)
                .ThenInclude(a => a.Challenges)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<LadderModel> GetByIdIncApprovedAsync(int id)
        {
            return await _context.LadderModel
                .Include(m => m.ApprovalUsersList)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<List<Ranking>> GetRankingsByLadderId(int id)
        {
            var ladder = await _context.LadderModel
                .Include(m => m.CurrentRankings)
                .ThenInclude(rank => rank.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            return ladder.CurrentRankings.OrderBy(rank => rank.Position).ToList();
        }

        public async Task<List<LadderModel>> GetAllAsync()
        {
            return await _context.LadderModel.ToListAsync();
        }

        public async Task<List<LadderModel>> GetAllAsyncIncludes()
        {
            return await _context.LadderModel.Include(l => l.CurrentRankings)
                .ThenInclude(r => r.User)
                .ToListAsync();
        }

        public async Task<LadderModel> AddAsync(LadderModel ladder)
        {
            _context.LadderModel.Add(ladder);
            await _context.SaveChangesAsync();
            return ladder;
        }

        public async Task<LadderModel> UpdateAsync(LadderModel ladder)
        {
            _context.LadderModel.Update(ladder);
            await _context.SaveChangesAsync();
            return ladder;
        }

        public async Task<LadderModel> DeleteAsync(LadderModel ladder, IEnumerable<Challenge> allChallenges)
        {
            foreach (var rank in ladder.CurrentRankings)
            {
                rank.User.CurrentRanking = null;
                rank.LadderModel = null;
                rank.LadderModelId = null;
            }

            _context.Challenge.RemoveRange(allChallenges);
            _context.Ranking.RemoveRange(ladder.CurrentRankings);
            _context.LadderModel.Remove(ladder);
            await _context.SaveChangesAsync();
            return ladder;
        }

        public async Task<LadderModel> UpdateLadder(Challenge challenge)
        {
            var ladderModel = challenge.Ladder;

            var challenger = ladderModel.CurrentRankings.FirstOrDefault(a => a.User == challenge.Challenger);
            var challengee = ladderModel.CurrentRankings.FirstOrDefault(a => a.User == challenge.Challengee);

            if (challengee == null || challenger == null) return ladderModel;
            var topPos = Math.Max(challenger.Position, challengee.Position);
            var bottomPos = Math.Min(challenger.Position, challengee.Position);

            switch (challenge.Result)
            {
                case Winner.Challenger:
                {
                    challengee.Losses++;
                    challenger.Wins++;
                    challenger.Position = bottomPos;
                    challengee.Position = topPos;

                    break;
                }
                case Winner.Challengee:
                    challengee.Wins++;
                    challenger.Losses++;
                    challenger.Position = topPos;
                    challengee.Position = bottomPos;
                    break;
            }

            _context.Ranking.UpdateRange(ladderModel.CurrentRankings);
            await _context.SaveChangesAsync();

            return ladderModel;
        }

        public Ranking GetRankByIdAsync(LadderModel ladder, int userId)
        {
            return ladder.CurrentRankings.FirstOrDefault(a => a?.User?.Id == userId);
        }

        public bool Exists(int id)
        {
            return _context.LadderModel.Any(l => l.Id == id);
        }
    }
}