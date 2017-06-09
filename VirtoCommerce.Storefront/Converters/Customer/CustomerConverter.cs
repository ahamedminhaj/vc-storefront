﻿using System.Linq;
using Microsoft.Practices.ServiceLocation;
using Omu.ValueInjecter;
using VirtoCommerce.Storefront.Common;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Customer;
using VirtoCommerce.Storefront.Model.Stores;
using coreDto = VirtoCommerce.Storefront.AutoRestClients.CoreModuleApi.Models;
using customerDto = VirtoCommerce.Storefront.AutoRestClients.CustomerModuleApi.Models;

namespace VirtoCommerce.Storefront.Converters
{
    public static class CustomerConverterExtension
    {
        public static CustomerConverter CustomerConverterInstance => ServiceLocator.Current.GetInstance<CustomerConverter>();

        public static CustomerInfo ToCustomerInfo(this Register formModel)
        {
            return CustomerConverterInstance.ToCustomerInfo(formModel);
        }

        public static CustomerInfo ToCustomerInfo(this customerDto.Contact contactDto)
        {
            return CustomerConverterInstance.ToCustomerInfo(contactDto);
        }

        public static customerDto.Contact ToCustomerContactDto(this CustomerInfo customer)
        {
            return CustomerConverterInstance.ToCustomerContactDto(customer);
        }

        public static coreDto.Contact ToCoreContactDto(this CustomerInfo customer)
        {
            return CustomerConverterInstance.ToCoreContactDto(customer);
        }

        public static Vendor ToVendor(this customerDto.Vendor vendorDto, Language currentLanguage, Store store)
        {
            return CustomerConverterInstance.ToVendor(vendorDto, currentLanguage, store);
        }

        public static Address ToAddress(this customerDto.Address addressDto)
        {
            return CustomerConverterInstance.ToAddress(addressDto);
        }

        public static customerDto.Address ToCustomerAddressDto(this Address address)
        {
            return CustomerConverterInstance.ToCustomerAddressDto(address);
        }

        public static DynamicProperty ToDynamicProperty(this customerDto.DynamicObjectProperty propertyDto)
        {
            return CustomerConverterInstance.ToDynamicProperty(propertyDto);
        }

        public static customerDto.DynamicObjectProperty ToCustomerDynamicPropertyDto(this DynamicProperty property)
        {
            return CustomerConverterInstance.ToCustomerDynamicPropertyDto(property);
        }

        public static Organization ToOrganization(this customerDto.Organization organizationDto)
        {
            return CustomerConverterInstance.ToOrganization(organizationDto);
        }

        public static customerDto.Organization ToCustomerOrganization(this Organization organization)
        {
            return CustomerConverterInstance.ToCustomerOrganization(organization);
        }

        public static customerDto.MembersSearchCriteria ToMembersSearchCriteriaDto(this OrganizationMembersSearchCriteria criteria)
        {
            return CustomerConverterInstance.ToMembersSearchCriteriaDto(criteria);
        }

        public static MemberInfo ToMemberCustomerInfoDto(this customerDto.Member member)
        {
            return CustomerConverterInstance.ToMemberCustomerInfoDto(member);
        }
    }

    public class CustomerConverter
    {
        private static readonly char[] _nameSeparator = { ' ' };


        public virtual DynamicProperty ToDynamicProperty(customerDto.DynamicObjectProperty propertyDto)
        {
            return propertyDto.JsonConvert<coreDto.DynamicObjectProperty>().ToDynamicProperty();
        }

        public virtual customerDto.DynamicObjectProperty ToCustomerDynamicPropertyDto(DynamicProperty property)
        {
            return property.ToDynamicPropertyDto().JsonConvert<customerDto.DynamicObjectProperty>();
        }

        public virtual Address ToAddress(customerDto.Address addressDto)
        {
            return addressDto.JsonConvert<coreDto.Address>().ToAddress();
        }

        public virtual customerDto.Address ToCustomerAddressDto(Address address)
        {
            return address.ToCoreAddressDto().JsonConvert<customerDto.Address>();
        }

        public virtual Vendor ToVendor(customerDto.Vendor vendorDto, Language currentLanguage, Store store)
        {
            Vendor result = null;

            if (vendorDto != null)
            {
                result = new Vendor();
                result.Id = vendorDto.Id;
                result.Name = vendorDto.Name;
                result.Description = vendorDto.Description;
                result.LogoUrl = vendorDto.LogoUrl;
                result.SiteUrl = vendorDto.SiteUrl;
                result.GroupName = vendorDto.GroupName;

                if (!vendorDto.SeoInfos.IsNullOrEmpty())
                {
                    var seoInfoDto = vendorDto.SeoInfos.Select(x => x.JsonConvert<coreDto.SeoInfo>())
                        .GetBestMatchingSeoInfos(store, currentLanguage)
                        .FirstOrDefault();

                    if (seoInfoDto != null)
                    {
                        result.SeoInfo = seoInfoDto.ToSeoInfo();
                    }
                }

                if (result.SeoInfo == null)
                {
                    result.SeoInfo = new SeoInfo
                    {
                        Title = vendorDto.Name,
                        Slug = string.Concat("/vendor/", result.Id)
                    };
                }

                if (vendorDto.Addresses != null)
                {
                    result.Addresses = vendorDto.Addresses.Select(ToAddress).ToList();
                }

                if (vendorDto.DynamicProperties != null)
                {
                    result.DynamicProperties = vendorDto.DynamicProperties.Select(ToDynamicProperty).ToList();
                }
            }

            return result;
        }

        public virtual CustomerInfo ToCustomerInfo(Register formModel)
        {
            var result = new CustomerInfo();
            result.Email = formModel.Email;
            result.FullName = string.Join(" ", formModel.FirstName, formModel.LastName);
            result.FirstName = formModel.FirstName;
            result.LastName = formModel.LastName;
            result.AccountType = formModel.AccountType;
            result.CompanyName = formModel.CompanyName;

            if (string.IsNullOrEmpty(result.FullName) || string.IsNullOrWhiteSpace(result.FullName))
            {
                result.FullName = formModel.Email;
            }
            return result;
        }

        public virtual CustomerInfo ToCustomerInfo(customerDto.Contact contactDto)
        {
            var result = new CustomerInfo();
            result.InjectFrom<NullableAndEnumValueInjecter>(contactDto);
            result.UserGroups = contactDto.Groups;
            result.IsRegisteredUser = true;
            if (contactDto.Addresses != null)
            {
                result.Addresses = contactDto.Addresses.Select(ToAddress).ToList();
            }

            result.DefaultBillingAddress = result.Addresses.FirstOrDefault(a => (a.Type & AddressType.Billing) == AddressType.Billing);
            result.DefaultShippingAddress = result.Addresses.FirstOrDefault(a => (a.Type & AddressType.Shipping) == AddressType.Shipping);

            // TODO: Need separate properties for first, middle and last name
            if (!string.IsNullOrEmpty(contactDto.FullName))
            {
                var nameParts = contactDto.FullName.Split(_nameSeparator, 2);

                if (nameParts.Length > 0)
                {
                    result.FirstName = nameParts[0];
                }

                if (nameParts.Length > 1)
                {
                    result.LastName = nameParts[1];
                }
            }

            if (contactDto.Emails != null)
            {
                result.Email = contactDto.Emails.FirstOrDefault();
            }
            if (!contactDto.DynamicProperties.IsNullOrEmpty())
            {
                result.DynamicProperties = contactDto.DynamicProperties.Select(ToDynamicProperty).ToList();
            }

            return result;
        }

        public virtual customerDto.Contact ToCustomerContactDto(CustomerInfo customer)
        {
            var retVal = new customerDto.Contact();
            retVal.InjectFrom<NullableAndEnumValueInjecter>(customer);

            if (customer.UserGroups != null)
            {
                retVal.Groups = customer.UserGroups.ToArray();
            }
            if (customer.Addresses != null)
            {
                retVal.Addresses = customer.Addresses.Select(ToCustomerAddressDto).ToList();
            }
            if (!string.IsNullOrEmpty(customer.Email))
            {
                retVal.Emails = new[] { customer.Email }.ToList();
            }
            retVal.FullName = customer.FullName;

            return retVal;
        }

        public virtual coreDto.Contact ToCoreContactDto(CustomerInfo customer)
        {
            var retVal = new coreDto.Contact();
            retVal.InjectFrom<NullableAndEnumValueInjecter>(customer);

            if (customer.UserGroups != null)
            {
                retVal.Groups = customer.UserGroups.ToArray();
            }
            if (customer.Addresses != null)
            {
                retVal.Addresses = customer.Addresses.Select(x => x.ToCoreAddressDto()).ToList();
            }
            if (!string.IsNullOrEmpty(customer.Email))
            {
                retVal.Emails = new[] { customer.Email }.ToList();
            }
            retVal.FullName = customer.FullName;

            return retVal;
        }

        public virtual Organization ToOrganization(customerDto.Organization organizationDto)
        {
            Organization result = null;

            if (organizationDto != null)
            {
                result = new Organization();
                result.Id = organizationDto.Id;
                result.Name = organizationDto.Name;
                result.Description = organizationDto.Description;

                if (organizationDto.Addresses != null)
                {
                    result.Addresses = organizationDto.Addresses.Select(ToAddress).ToList();
                }

                if (organizationDto.DynamicProperties != null)
                {
                    result.DynamicProperties = organizationDto.DynamicProperties.Select(ToDynamicProperty).ToList();
                }
            }

            return result;
        }

        public virtual customerDto.Organization ToCustomerOrganization(Organization organization)
        {
            var retVal = new customerDto.Organization();
            retVal.InjectFrom<NullableAndEnumValueInjecter>(organization);
           
            if (organization.Addresses != null)
            {
                retVal.Addresses = organization.Addresses.Select(ToCustomerAddressDto).ToList();
            }

            if (organization.DynamicProperties != null)
            {
                retVal.DynamicProperties = organization.DynamicProperties.Select(ToCustomerDynamicPropertyDto).ToList();
            }

            return retVal;
        }

        public virtual customerDto.MembersSearchCriteria ToMembersSearchCriteriaDto(OrganizationMembersSearchCriteria criteria)
        {
            var result = new customerDto.MembersSearchCriteria();

            result.InjectFrom(criteria);

            result.Skip = criteria.Start;
            result.Take = criteria.PageSize;
            result.Sort = criteria.Sort;

            return result;
        }

        public virtual MemberInfo ToMemberCustomerInfoDto(customerDto.Member member)
        {
            var result = new MemberInfo();
            result.InjectFrom<NullableAndEnumValueInjecter>(member);
            
            if (member.Addresses != null)
            {
                result.Addresses = member.Addresses.Select(ToAddress).ToList();
            }

            return result;
        }
    }
}
