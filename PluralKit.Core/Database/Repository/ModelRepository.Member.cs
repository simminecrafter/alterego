#nullable enable
using System;
using System.Data;
using System.Threading.Tasks;

using Dapper;

namespace PluralKit.Core
{
    public partial class ModelRepository
    {
        public Task<PKMember?> GetMember(IPKConnection conn, MemberId id) =>
            conn.QueryFirstOrDefaultAsync<PKMember?>("select * from members where id = @id", new { id });

        public Task<PKMember?> GetMemberByHid(IPKConnection conn, string hid, SystemId? system = null)
            => conn.QuerySingleOrDefaultAsync<PKMember?>(
                "select * from members where hid = @Hid" + (system != null ? " and system = @System" : ""),
                new { Hid = hid.ToLower(), System = system }
            );

        public Task<PKMember?> GetMemberByName(IPKConnection conn, SystemId system, string name) =>
            conn.QueryFirstOrDefaultAsync<PKMember?>("select * from members where lower(name) = lower(@Name) and system = @SystemID", new { Name = name, SystemID = system });

        public Task<PKMember?> GetMemberByDisplayName(IPKConnection conn, SystemId system, string name) =>
            conn.QueryFirstOrDefaultAsync<PKMember?>("select * from members where lower(display_name) = lower(@Name) and system = @SystemID", new { Name = name, SystemID = system });

        public async Task<PKMember> CreateMember(IPKConnection conn, SystemId id, string memberName, IDbTransaction? transaction = null)
        {
            var member = await conn.QueryFirstAsync<PKMember>(
                "insert into members (hid, system, name) values (find_free_member_hid(), @SystemId, @Name) returning *",
                new { SystemId = id, Name = memberName }, transaction);
            _logger.Information("Created {MemberId} in {SystemId}: {MemberName}",
                member.Id, id, memberName);
            return member;
        }

        public Task<PKMember> UpdateMember(IPKConnection conn, MemberId id, MemberPatch patch, IDbTransaction? transaction = null)
        {
            _logger.Information("Updated {MemberId}: {@MemberPatch}", id, patch);
            var (query, pms) = patch.Apply(UpdateQueryBuilder.Update("members", "id = @id"))
                .WithConstant("id", id)
                .Build("returning *");
            return conn.QueryFirstAsync<PKMember>(query, pms, transaction);
        }

        public Task DeleteMember(IPKConnection conn, MemberId id)
        {
            _logger.Information("Deleted {MemberId}", id);
            return conn.ExecuteAsync("delete from members where id = @Id", new { Id = id });
        }
    }
}