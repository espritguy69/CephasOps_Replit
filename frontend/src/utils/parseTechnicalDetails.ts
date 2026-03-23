/**
 * Parses technical details from remarks/PartnerNotes string
 * Extracts technical details, Internet network details, and VOIP network details
 */
export interface TechnicalDetails {
  // Basic technical details
  onuPassword?: string;
  username?: string;
  password?: string;
  splitterLocation?: string;
  
  // Internet network details
  internetWanIp?: string;
  internetLanIp?: string;
  internetGateway?: string;
  internetSubnetMask?: string;
  
  // VOIP network details
  voipServiceId?: string;
  voipPassword?: string;
  voipOnuIp?: string;
  voipGateway?: string;
  voipSubnetMask?: string;
  voipSrpIp?: string;
  
  // Other remarks (non-technical)
  otherRemarks?: string;
}

export const parseTechnicalDetails = (remarks?: string | null): TechnicalDetails => {
  if (!remarks) {
    return {};
  }

  const result: TechnicalDetails = {};
  const lines = remarks.split('\n');
  
  let inTechnicalSection = false;
  let inInternetSection = false;
  let inVoipSection = false;
  const otherRemarks: string[] = [];

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].trim();
    
    // Detect section starts
    if (line.includes('Technical Details:') && !line.includes('Internet') && !line.includes('VOIP')) {
      inTechnicalSection = true;
      inInternetSection = false;
      inVoipSection = false;
      continue;
    }
    
    if (line.includes('Technical Details – Internet:') || line.includes('Technical Details - Internet:')) {
      inInternetSection = true;
      inTechnicalSection = false;
      inVoipSection = false;
      continue;
    }
    
    if (line.includes('Technical Details – VOIP:') || line.includes('Technical Details - VOIP:')) {
      inVoipSection = true;
      inTechnicalSection = false;
      inInternetSection = false;
      continue;
    }

    // Detect section end (empty line or new section)
    if ((inTechnicalSection || inInternetSection || inVoipSection) && 
        (line === '' || line.startsWith('Technical Details') || line.startsWith('---'))) {
      if (line !== '' && !line.startsWith('Technical Details')) {
        otherRemarks.push(line);
      }
      inTechnicalSection = false;
      inInternetSection = false;
      inVoipSection = false;
      continue;
    }

    // Parse technical detail lines
    if (inTechnicalSection) {
      if (line.toLowerCase().startsWith('onu password:')) {
        result.onuPassword = line.substring('onu password:'.length).trim();
      } else if (line.toLowerCase().startsWith('username:')) {
        result.username = line.substring('username:'.length).trim();
      } else if (line.toLowerCase().startsWith('password:')) {
        result.password = line.substring('password:'.length).trim();
      } else if (line.toLowerCase().startsWith('splitter location:')) {
        result.splitterLocation = line.substring('splitter location:'.length).trim();
      }
    } else if (inInternetSection) {
      if (line.toLowerCase().startsWith('wan ip:')) {
        result.internetWanIp = line.substring('wan ip:'.length).trim();
      } else if (line.toLowerCase().startsWith('lan ip:')) {
        result.internetLanIp = line.substring('lan ip:'.length).trim();
      } else if (line.toLowerCase().startsWith('gateway:')) {
        result.internetGateway = line.substring('gateway:'.length).trim();
      } else if (line.toLowerCase().startsWith('subnet mask:')) {
        result.internetSubnetMask = line.substring('subnet mask:'.length).trim();
      }
    } else if (inVoipSection) {
      if (line.toLowerCase().startsWith('service id:')) {
        result.voipServiceId = line.substring('service id:'.length).trim();
      } else if (line.toLowerCase().startsWith('password:')) {
        result.voipPassword = line.substring('password:'.length).trim();
      } else if (line.toLowerCase().startsWith('ip address onu:') || line.toLowerCase().startsWith('ip address onu')) {
        result.voipOnuIp = line.substring(line.toLowerCase().indexOf(':') + 1).trim();
      } else if (line.toLowerCase().startsWith('gateway onu:')) {
        result.voipGateway = line.substring('gateway onu:'.length).trim();
      } else if (line.toLowerCase().startsWith('subnet mask onu:')) {
        result.voipSubnetMask = line.substring('subnet mask onu:'.length).trim();
      } else if (line.toLowerCase().startsWith('ip address srp:') || line.toLowerCase().startsWith('ip address srp')) {
        result.voipSrpIp = line.substring(line.toLowerCase().indexOf(':') + 1).trim();
      }
    } else {
      // Collect other remarks
      if (line && !line.startsWith('Technical Details')) {
        otherRemarks.push(line);
      }
    }
  }

  if (otherRemarks.length > 0) {
    result.otherRemarks = otherRemarks.join('\n').trim();
  }

  return result;
};

