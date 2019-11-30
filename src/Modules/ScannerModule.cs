﻿using BotTerminator.Configuration;
using BotTerminator.Models;
using Newtonsoft.Json.Linq;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotTerminator.Modules
{
	public class ScannerModule<T> : ListingBotModule<T> where T : ModeratableThing
	{
		public ScannerModule(BotTerminator bot, Listing<T> listing) : base(bot, listing)
		{
		}

		public override Task SetupAsync() => Task.CompletedTask;

		public override Task TeardownAsync() => Task.CompletedTask;

		protected override Task PostRunItemsAsync(ICollection<T> things) => Task.CompletedTask;

		protected override sealed Boolean PreRunItem(T thing)
		{
			if (BotTerminator.IsUnbannable(thing) ||
				   thing.BannedBy != null || thing.BannedBy == RedditInstance.User.Name ||
				   (!GlobalConfig.AllowNsfw && thing["over_18"].Value<bool?>().GetValueOrDefault(false)) ||
				   (!GlobalConfig.AllowQuarantined && thing["quarantine"].Value<bool?>().GetValueOrDefault(false))) return false;
			// all distinguishes are given to moderators (who can't be banned) or known humans
			return thing.Distinguished == ModeratableThing.DistinguishType.None;
		}

		protected override sealed async Task RunItemAsync(T thing)
		{
			if (await bot.CheckShouldBanAsync(thing))
			{
				AbstractSubredditOptionSet options = GlobalConfig.GlobalOptions;
				if (!options.Enabled) return;
				try
				{
					if (options.RemovalType == RemovalType.Spam)
					{
						await thing.RemoveSpamAsync();
					}
					else
					{
						await thing.RemoveAsync();
					}
				}
				catch (RedditHttpException ex)
				{
					Console.WriteLine("Could not remove thing {0} due to HTTP error from reddit: {1}", thing.FullName, ex.Message);
				}
				if (options.BanDuration > -1)
				{
					await bot.SubredditLookup[thing["subreddit"].Value<String>()].BanUserAsync(thing.AuthorName, options.BanNote, null, options.BanDuration, options.BanMessage);
				}
			}
		}
	}
}