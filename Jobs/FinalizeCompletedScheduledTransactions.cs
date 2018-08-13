using System;
using System.Collections.Generic;
using System.Linq;

using Quartz;

using Rock;
using Rock.Data;
using Rock.Model;

namespace com.shepherdchurch.Misc.Jobs
{
    [DisallowConcurrentExecution]
    public class FinalizeCompletedScheduledTransactions : IJob
    {
        public virtual void Execute( IJobExecutionContext context )
        {
            List<string> messages = new List<string>();
            int completedCount = 0;

            using ( var rockContext = new RockContext() )
            {
                var txnService = new FinancialScheduledTransactionService( rockContext );
                var txns = txnService
                    .Queryable( "FinancialGateway" )
                    .Where( t => t.IsActive )
                    .ToList();

                foreach ( var txn in txns )
                {
                    txn.LoadAttributes( rockContext );

                    var maxCount = txn.GetAttributeValue( "com.shepherdchurch.MaxPaymentCount" ).AsInteger();

                    if ( maxCount <= 0 || txn.Transactions.Count < maxCount )
                    {
                        continue;
                    }

                    string errorMessage = string.Empty;
                    if ( txnService.Cancel( txn, out errorMessage ) )
                    {
                        txnService.GetStatus( txn, out errorMessage );
                        rockContext.SaveChanges();
                        completedCount += 1;
                    }
                    else
                    {
                        messages.Add( errorMessage );
                    }
                }
            }

            if (messages.Any())
            {
                throw new Exception( string.Join( "\r\n", messages ) );
            }

            context.Result = string.Format( "Marked {0} scheduled transactions as complete.", completedCount );
        }
    }
}
