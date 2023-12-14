﻿using Bit.Core.Enums;
using Bit.Infrastructure.EntityFramework.Repositories;
using Bit.Infrastructure.EntityFramework.Repositories.Queries;
using Bit.Infrastructure.EntityFramework.Vault.Models;

namespace Bit.Infrastructure.EntityFramework.Vault.Repositories.Queries;

public class CipherReadCanEditByIdUserIdQuery : IQuery<Cipher>
{
    private readonly Guid _userId;
    private readonly Guid _cipherId;
    private readonly bool _useFlexibleCollections;

    public CipherReadCanEditByIdUserIdQuery(Guid userId, Guid cipherId, bool useFlexibleCollections)
    {
        _userId = userId;
        _cipherId = cipherId;
        _useFlexibleCollections = useFlexibleCollections;
    }

    public virtual IQueryable<Cipher> Run(DatabaseContext dbContext)
    {
        return _useFlexibleCollections
            ? Run_vNext(dbContext)
            : Run_Legacy(dbContext);
    }

    private IQueryable<Cipher> Run_vNext(DatabaseContext dbContext)
    {
        var query = from c in dbContext.Ciphers

                    join o in dbContext.Organizations
                        on new { c.UserId, c.OrganizationId } equals
                           new { UserId = (Guid?)null, OrganizationId = (Guid?)o.Id } into o_g
                    from o in o_g.DefaultIfEmpty()

                    join ou in dbContext.OrganizationUsers
                        on new { OrganizationId = o.Id, UserId = (Guid?)_userId } equals
                           new { ou.OrganizationId, ou.UserId } into ou_g
                    from ou in ou_g.DefaultIfEmpty()

                    join cc in dbContext.CollectionCiphers
                        on new { c.UserId, CipherId = c.Id } equals
                           new { UserId = (Guid?)null, cc.CipherId } into cc_g
                    from cc in cc_g.DefaultIfEmpty()

                    join cu in dbContext.CollectionUsers
                        on new { cc.CollectionId, OrganizationUserId = ou.Id } equals
                           new { cu.CollectionId, cu.OrganizationUserId } into cu_g
                    from cu in cu_g.DefaultIfEmpty()

                    join gu in dbContext.GroupUsers
                        on new { c.UserId, CollectionId = (Guid?)cu.CollectionId, OrganizationUserId = ou.Id } equals
                           new { UserId = (Guid?)null, CollectionId = (Guid?)null, gu.OrganizationUserId } into gu_g
                    from gu in gu_g.DefaultIfEmpty()

                    join g in dbContext.Groups
                        on gu.GroupId equals g.Id into g_g
                    from g in g_g.DefaultIfEmpty()

                    join cg in dbContext.CollectionGroups
                        on new { cc.CollectionId, gu.GroupId } equals
                           new { cg.CollectionId, cg.GroupId } into cg_g
                    from cg in cg_g.DefaultIfEmpty()

                    where
                    c.Id == _cipherId &&
                    (
                        c.UserId == _userId ||
                        (
                            !c.UserId.HasValue && ou.Status == OrganizationUserStatusType.Confirmed && o.Enabled &&
                            (cu.CollectionId != null || cg.CollectionId != null)
                        )
                    ) &&
                    (c.UserId.HasValue || !cu.ReadOnly || !cg.ReadOnly)
                    select c;
        return query;
    }

    private IQueryable<Cipher> Run_Legacy(DatabaseContext dbContext)
    {
        var query = from c in dbContext.Ciphers

                    join o in dbContext.Organizations
                        on new { c.UserId, c.OrganizationId } equals
                           new { UserId = (Guid?)null, OrganizationId = (Guid?)o.Id } into o_g
                    from o in o_g.DefaultIfEmpty()

                    join ou in dbContext.OrganizationUsers
                        on new { OrganizationId = o.Id, UserId = (Guid?)_userId } equals
                           new { ou.OrganizationId, ou.UserId } into ou_g
                    from ou in ou_g.DefaultIfEmpty()

                    join cc in dbContext.CollectionCiphers
                        on new { c.UserId, ou.AccessAll, CipherId = c.Id } equals
                           new { UserId = (Guid?)null, AccessAll = false, cc.CipherId } into cc_g
                    from cc in cc_g.DefaultIfEmpty()

                    join cu in dbContext.CollectionUsers
                        on new { cc.CollectionId, OrganizationUserId = ou.Id } equals
                           new { cu.CollectionId, cu.OrganizationUserId } into cu_g
                    from cu in cu_g.DefaultIfEmpty()

                    join gu in dbContext.GroupUsers
                        on new { c.UserId, CollectionId = (Guid?)cu.CollectionId, ou.AccessAll, OrganizationUserId = ou.Id } equals
                           new { UserId = (Guid?)null, CollectionId = (Guid?)null, AccessAll = false, gu.OrganizationUserId } into gu_g
                    from gu in gu_g.DefaultIfEmpty()

                    join g in dbContext.Groups
                        on gu.GroupId equals g.Id into g_g
                    from g in g_g.DefaultIfEmpty()

                    join cg in dbContext.CollectionGroups
                        on new { g.AccessAll, cc.CollectionId, gu.GroupId } equals
                           new { AccessAll = false, cg.CollectionId, cg.GroupId } into cg_g
                    from cg in cg_g.DefaultIfEmpty()

                    where
                    c.Id == _cipherId &&
                    (
                        c.UserId == _userId ||
                        (
                            !c.UserId.HasValue && ou.Status == OrganizationUserStatusType.Confirmed && o.Enabled &&
                            (ou.AccessAll || cu.CollectionId != null || g.AccessAll || cg.CollectionId != null)
                        )
                    ) &&
                    (c.UserId.HasValue || ou.AccessAll || !cu.ReadOnly || g.AccessAll || !cg.ReadOnly)
                    select c;
        return query;
    }
}
